using System.Collections.Immutable;
using System.Composition;
using Darp.Results.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Darp.Results.CodeFixers.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class ExcludeDocumentedMemberCodeFixer : CodeFixProvider
{
    private const string ExcludedMembersOption = "dotnet_code_quality.DR0005.excluded_members";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        [RuleIdentifiers.DocumentedExceptionMayEscapeIdentifier];

    public override FixAllProvider? GetFixAllProvider() => null;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        Diagnostic? diagnostic = context.Diagnostics.FirstOrDefault();
        if (
            diagnostic is null
            || !diagnostic.Properties.TryGetValue("MemberDocumentationId", out string? memberId)
            || string.IsNullOrEmpty(memberId)
            || FindNearestEditorConfig(context.Document) is not { } editorConfig
        )
        {
            return Task.CompletedTask;
        }

        diagnostic.Properties.TryGetValue("ConfiguredExcludedMembers", out string? configuredMembers);
        context.RegisterCodeFix(
            CodeAction.Create(
                $"Exclude '{memberId}' from DR0005",
                cancellationToken =>
                    AddExcludedMemberAsync(
                        context.Document.Project.Solution,
                        editorConfig.Id,
                        memberId!,
                        configuredMembers,
                        cancellationToken
                    ),
                equivalenceKey: nameof(ExcludeDocumentedMemberCodeFixer)
            ),
            diagnostic
        );
        return Task.CompletedTask;
    }

    private static AnalyzerConfigDocument? FindNearestEditorConfig(Document document)
    {
        if (document.FilePath is null)
            return document.Project.AnalyzerConfigDocuments.FirstOrDefault();

        string sourcePath = NormalizePath(document.FilePath);
        return document
            .Project.AnalyzerConfigDocuments.Select(config =>
                (Config: config, Directory: GetEditorConfigDirectory(config.FilePath))
            )
            .Where(candidate =>
                candidate.Directory is not null
                && (
                    candidate.Directory.Length == 0
                    || sourcePath.StartsWith(candidate.Directory + "/", StringComparison.OrdinalIgnoreCase)
                )
            )
            .OrderByDescending(candidate => candidate.Directory!.Length)
            .Select(candidate => candidate.Config)
            .FirstOrDefault();
    }

    private static async Task<Solution> AddExcludedMemberAsync(
        Solution solution,
        DocumentId editorConfigId,
        string memberId,
        string? configuredMembers,
        CancellationToken cancellationToken
    )
    {
        AnalyzerConfigDocument? editorConfig = solution.GetAnalyzerConfigDocument(editorConfigId);
        if (editorConfig is null)
            return solution;

        SourceText text = await editorConfig.GetTextAsync(cancellationToken).ConfigureAwait(false);
        SourceText updatedText = UpdateEditorConfig(text, configuredMembers, memberId);
        return solution.WithAnalyzerConfigDocumentText(editorConfigId, updatedText);
    }

    internal static SourceText UpdateEditorConfig(SourceText text, string? configuredMembers, string memberId)
    {
        string currentValue = NormalizeMembers(configuredMembers);
        (TextLine Line, string Value)? exactSetting = null;
        (TextLine Line, string Value)? updatedSetting = null;
        foreach (TextLine line in text.Lines)
        {
            if (!TryGetSettingValue(text.ToString(line.Span), out string? value))
                continue;

            string normalizedValue = NormalizeMembers(value);
            if (normalizedValue == currentValue)
                exactSetting = (line, normalizedValue);
            else if (ContainsAllMembers(normalizedValue, currentValue))
                updatedSetting = (line, normalizedValue);
        }

        if ((exactSetting ?? updatedSetting) is { } existingSetting)
        {
            string updatedValue = NormalizeMembers(existingSetting.Value, memberId);
            string oldLine = text.ToString(existingSetting.Line.Span);
            string indentation = oldLine.Substring(0, oldLine.Length - oldLine.TrimStart().Length);
            return text.WithChanges(
                new TextChange(existingSetting.Line.Span, indentation + ExcludedMembersOption + " = " + updatedValue)
            );
        }

        string newValue = NormalizeMembers(configuredMembers, memberId);
        string newLine = GetNewLine(text);
        string setting = ExcludedMembersOption + " = " + newValue;
        if (FindLastCSharpSectionEnd(text) is { } position)
        {
            bool endsWithLineBreak = text.Length > 0 && text[text.Length - 1] is '\r' or '\n';
            string insertion =
                position < text.Length
                    ? setting + newLine
                    : (endsWithLineBreak ? string.Empty : newLine)
                        + setting
                        + (endsWithLineBreak ? newLine : string.Empty);
            return text.WithChanges(new TextChange(new TextSpan(position, 0), insertion));
        }

        bool hasFinalLineBreak = text.Length > 0 && text[text.Length - 1] is '\r' or '\n';
        string sectionPrefix =
            text.Length == 0 ? string.Empty
            : hasFinalLineBreak ? newLine
            : newLine + newLine;
        string section = sectionPrefix + "[*.cs]" + newLine + setting + (hasFinalLineBreak ? newLine : string.Empty);
        return text.WithChanges(new TextChange(new TextSpan(text.Length, 0), section));
    }

    private static string NormalizeMembers(string? value, string? memberToAdd = null)
    {
        IEnumerable<string> members = (value ?? string.Empty).Split('|');
        if (!string.IsNullOrEmpty(memberToAdd))
            members = members.Append(memberToAdd!);
        return string.Join(
            "|",
            members.Select(member => member.Trim()).Where(member => member.Length > 0).Distinct(StringComparer.Ordinal)
        );
    }

    private static bool ContainsAllMembers(string value, string requiredValue)
    {
        string[] members = NormalizeMembers(value).Split('|');
        return NormalizeMembers(requiredValue)
            .Split('|')
            .Where(member => member.Length > 0)
            .All(member => members.Contains(member, StringComparer.Ordinal));
    }

    private static bool TryGetSettingValue(string line, out string? value)
    {
        int equalsIndex = line.IndexOf('=');
        if (
            equalsIndex < 0
            || !string.Equals(
                line.Substring(0, equalsIndex).Trim(),
                ExcludedMembersOption,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            value = null;
            return false;
        }

        value = line.Substring(equalsIndex + 1).Trim();
        return true;
    }

    private static int? FindLastCSharpSectionEnd(SourceText text)
    {
        bool isCSharpSection = false;
        int? currentSectionEnd = null;
        int? lastSectionEnd = null;
        foreach (TextLine line in text.Lines)
        {
            string content = text.ToString(line.Span).Trim();
            if (content.StartsWith("[", StringComparison.Ordinal) && content.EndsWith("]", StringComparison.Ordinal))
            {
                if (isCSharpSection)
                    lastSectionEnd = currentSectionEnd;
                string pattern = content.Substring(1, content.Length - 2).Replace(" ", string.Empty);
                isCSharpSection = pattern is "*.cs" or "*.{cs,csx}" or "{*.cs,*.csx}";
                currentSectionEnd = isCSharpSection ? line.EndIncludingLineBreak : null;
            }
            else if (isCSharpSection && content.Length > 0)
            {
                currentSectionEnd = line.EndIncludingLineBreak;
            }
        }
        return isCSharpSection ? currentSectionEnd : lastSectionEnd;
    }

    private static string GetNewLine(SourceText text)
    {
        foreach (TextLine line in text.Lines)
        {
            if (line.EndIncludingLineBreak > line.End)
                return text.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak));
        }
        return "\n";
    }

    private static string? GetEditorConfigDirectory(string? filePath)
    {
        if (filePath is null || !filePath.EndsWith(".editorconfig", StringComparison.OrdinalIgnoreCase))
            return null;
        string path = NormalizePath(filePath);
        int separatorIndex = path.LastIndexOf('/');
        return separatorIndex < 0 ? string.Empty : path.Substring(0, separatorIndex);
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');
}

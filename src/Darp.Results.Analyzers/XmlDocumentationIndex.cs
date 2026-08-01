using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Darp.Results.Analyzers;

internal static class XmlDocumentationIndex
{
    private static readonly ConditionalWeakTable<SourceText, Documentation> s_cache = new();

    public static ImmutableArray<string> GetExceptionIds(AdditionalText file, string memberId)
    {
        SourceText? text = file.GetText();
        if (text is null)
            return [];

        Documentation documentation = s_cache.GetValue(text, Parse);
        return documentation.ExceptionsByMember.TryGetValue(memberId, out var exceptionIds) ? exceptionIds : [];
    }

    private static Documentation Parse(SourceText text)
    {
        var builders = new Dictionary<string, ImmutableArray<string>.Builder>(StringComparer.Ordinal);
        try
        {
            using var stringReader = new StringReader(text.ToString());
            using XmlReader reader = XmlReader.Create(
                stringReader,
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }
            );

            string? memberId = null;
            int memberDepth = -1;
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "member")
                {
                    memberId = reader.GetAttribute("name");
                    memberDepth = reader.Depth;
                    if (reader.IsEmptyElement)
                        memberId = null;
                }
                else if (
                    reader.NodeType == XmlNodeType.Element
                    && reader.LocalName == "exception"
                    && memberId is not null
                    && reader.Depth == memberDepth + 1
                    && reader.GetAttribute("cref") is { } exceptionId
                )
                {
                    if (!builders.TryGetValue(memberId, out var builder))
                    {
                        builder = ImmutableArray.CreateBuilder<string>();
                        builders.Add(memberId, builder);
                    }
                    builder.Add(exceptionId);
                }
                else if (
                    reader.NodeType == XmlNodeType.EndElement
                    && reader.LocalName == "member"
                    && reader.Depth == memberDepth
                )
                {
                    memberId = null;
                }
            }
        }
        catch (XmlException)
        {
            return new Documentation(ImmutableDictionary<string, ImmutableArray<string>>.Empty);
        }

        var exceptionsByMember = ImmutableDictionary.CreateBuilder<string, ImmutableArray<string>>(
            StringComparer.Ordinal
        );
        foreach (KeyValuePair<string, ImmutableArray<string>.Builder> entry in builders)
            exceptionsByMember.Add(entry.Key, entry.Value.ToImmutable());
        return new Documentation(exceptionsByMember.ToImmutable());
    }

    private sealed class Documentation(ImmutableDictionary<string, ImmutableArray<string>> exceptionsByMember)
    {
        public ImmutableDictionary<string, ImmutableArray<string>> ExceptionsByMember { get; } = exceptionsByMember;
    }
}

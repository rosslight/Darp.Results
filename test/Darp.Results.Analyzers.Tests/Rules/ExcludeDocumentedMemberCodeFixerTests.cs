using Darp.Results.Analyzers.Rules;
using Darp.Results.CodeFixers.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace Darp.Results.Analyzers.Tests.Rules;

public sealed class ExcludeDocumentedMemberCodeFixerTests
{
    [Fact]
    public async Task Method_ShouldBeAppendedToExistingExcludedMembers()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            namespace Test;

            static class Dependency
            {
                /// <exception cref="IOException">Reading failed.</exception>
                internal static int Read(string path) => 0;
            }

            static class TestClass
            {
                static Result<int, string> Run() => {|DR0005:Dependency.Read("path")|};
            }
            """;
        const string fixedSource = """
            using Darp.Results;
            using System.IO;

            namespace Test;

            static class Dependency
            {
                /// <exception cref="IOException">Reading failed.</exception>
                internal static int Read(string path) => 0;
            }

            static class TestClass
            {
                static Result<int, string> Run() => Dependency.Read("path");
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            dotnet_code_quality.DR0005.excluded_members = P:System.Array.Length | P:System.String.Length
            """;
        const string fixedEditorConfig = """
            root = true

            [*.cs]
            dotnet_code_quality.DR0005.excluded_members = P:System.Array.Length|P:System.String.Length|M:Test.Dependency.Read(System.String)
            """;

        await VerifyAsync(source, fixedSource, editorConfig, fixedEditorConfig);
    }

    [Fact]
    public async Task Property_ShouldBeAddedToExistingCSharpSection()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            namespace Test;

            static class Dependency
            {
                /// <exception cref="IOException">Reading failed.</exception>
                internal static int Value => 0;
            }

            static class TestClass
            {
                static Result<int, string> Run() => {|DR0005:Dependency.Value|};
            }
            """;
        const string fixedSource = """
            using Darp.Results;
            using System.IO;

            namespace Test;

            static class Dependency
            {
                /// <exception cref="IOException">Reading failed.</exception>
                internal static int Value => 0;
            }

            static class TestClass
            {
                static Result<int, string> Run() => Dependency.Value;
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            dotnet_diagnostic.DR0005.severity = warning

            [*.md]
            trim_trailing_whitespace = false
            """;
        const string fixedEditorConfig = """
            root = true

            [*.cs]
            dotnet_diagnostic.DR0005.severity = warning
            dotnet_code_quality.DR0005.excluded_members = P:Test.Dependency.Value

            [*.md]
            trim_trailing_whitespace = false
            """;

        await VerifyAsync(source, fixedSource, editorConfig, fixedEditorConfig);
    }

    [Fact]
    public async Task MissingEditorConfig_ShouldNotOfferFix()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            static class Dependency
            {
                /// <exception cref="IOException">Reading failed.</exception>
                internal static int Read() => 0;
            }

            static class TestClass
            {
                static Result<int, string> Run() => {|DR0005:Dependency.Read()|};
            }
            """;

        var test = CreateTest(source);
        test.NumberOfIncrementalIterations = 0;
        await test.RunAsync(CancellationToken.None);
    }

    private static Task VerifyAsync(string source, string fixedSource, string editorConfig, string fixedEditorConfig)
    {
        CSharpCodeFixTest<
            DocumentedExceptionMayEscapeAnalyzer,
            ExcludeDocumentedMemberCodeFixer,
            DefaultVerifier
        > test = CreateTest(source);
        test.FixedCode = fixedSource;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
        test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", fixedEditorConfig));
        return test.RunAsync(CancellationToken.None);
    }

    private static CSharpCodeFixTest<
        DocumentedExceptionMayEscapeAnalyzer,
        ExcludeDocumentedMemberCodeFixer,
        DefaultVerifier
    > CreateTest(string source)
    {
        var test = new CSharpCodeFixTest<
            DocumentedExceptionMayEscapeAnalyzer,
            ExcludeDocumentedMemberCodeFixer,
            DefaultVerifier
        >
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck,
        };
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Result<,>).Assembly.Location));
        return test;
    }
}

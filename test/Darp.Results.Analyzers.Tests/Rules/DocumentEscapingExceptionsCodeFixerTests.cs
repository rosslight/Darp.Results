using Darp.Results.Analyzers.Rules;
using Darp.Results.CodeFixers.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace Darp.Results.Analyzers.Tests.Rules;

public sealed class DocumentEscapingExceptionsCodeFixerTests
{
    [Fact]
    public async Task DocumentedInvocation_ShouldAddAllEscapingExceptions()
    {
        const string source = """
            using Darp.Results;
            using System;
            using System.IO;

            static class Dependency
            {
                /// <exception cref="IOException">Reading failed.</exception>
                /// <exception cref="UnauthorizedAccessException">Access was denied.</exception>
                internal static int Read() => 0;
            }

            static class TestClass
            {
                static Result<int, string> Run() => {|DR0004:Dependency.Read()|};
            }
            """;
        const string fixedSource = """
            using Darp.Results;
            using System;
            using System.IO;

            static class Dependency
            {
                /// <exception cref="IOException">Reading failed.</exception>
                /// <exception cref="UnauthorizedAccessException">Access was denied.</exception>
                internal static int Read() => 0;
            }

            static class TestClass
            {
                /// <exception cref="global::System.IO.IOException"></exception>
                /// <exception cref="global::System.UnauthorizedAccessException"></exception>
                static Result<int, string> Run() => Dependency.Read();
            }
            """;

        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task ExistingDocumentation_ShouldBePreserved()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            static class TestClass
            {
                /// <summary>Reads a value.</summary>
                static Result<int, string> Run() => {|DR0004:throw new IOException()|};
            }
            """;
        const string fixedSource = """
            using Darp.Results;
            using System.IO;

            static class TestClass
            {
                /// <summary>Reads a value.</summary>
                /// <exception cref="global::System.IO.IOException"></exception>
                static Result<int, string> Run() => throw new IOException();
            }
            """;

        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task ResultReturningProperty_ShouldDocumentProperty()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            static class TestClass
            {
                static Result<int, string> Value => {|DR0004:throw new IOException()|};
            }
            """;
        const string fixedSource = """
            using Darp.Results;
            using System.IO;

            static class TestClass
            {
                /// <exception cref="global::System.IO.IOException"></exception>
                static Result<int, string> Value => throw new IOException();
            }
            """;

        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task MultipleDiagnosticsOnSameMethod_SuccessiveFixesShouldDocumentAllExceptions()
    {
        const string source = """
            using Darp.Results;
            using System;
            using System.IO;

            static class Dependency
            {
                /// <exception cref="IOException">Reading failed.</exception>
                internal static int Read() => 0;

                /// <exception cref="InvalidOperationException">Validation failed.</exception>
                internal static int Validate(int value) => value;
            }

            static class TestClass
            {
                static Result<int, string> Run()
                {
                    int value = {|DR0004:Dependency.Read()|};
                    return {|DR0004:Dependency.Validate(value)|};
                }
            }
            """;
        const string fixedSource = """
            using Darp.Results;
            using System;
            using System.IO;

            static class Dependency
            {
                /// <exception cref="IOException">Reading failed.</exception>
                internal static int Read() => 0;

                /// <exception cref="InvalidOperationException">Validation failed.</exception>
                internal static int Validate(int value) => value;
            }

            static class TestClass
            {
                /// <exception cref="global::System.IO.IOException"></exception>
                /// <exception cref="global::System.InvalidOperationException"></exception>
                static Result<int, string> Run()
                {
                    int value = Dependency.Read();
                    return Dependency.Validate(value);
                }
            }
            """;

        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task MemberFollowingCodeOnSameLine_ShouldStartDocumentationOnNewLine()
    {
        string source = """
            using Darp.Results;
            using System.IO;

            static class TestClass { static Result<int, string> Run() => {|DR0004:throw new IOException()|}; }
            """.ReplaceLineEndings(Environment.NewLine);
        string fixedSource = """
            using Darp.Results;
            using System.IO;

            static class TestClass {
                /// <exception cref="global::System.IO.IOException"></exception>
                static Result<int, string> Run() => throw new IOException();
            }
            """.ReplaceLineEndings(Environment.NewLine);

        await VerifyAsync(source, fixedSource);
    }

    [Fact]
    public async Task ShadowedRootNamespace_ShouldUseGloballyQualifiedCref()
    {
        const string source = """
            using Darp.Results;
            using IOException = global::System.IO.IOException;
            using System = MyCompany.System;

            namespace MyCompany.System
            {
                sealed class Placeholder { }
            }

            static class TestClass
            {
                static Result<int, string> Run() => {|DR0004:throw new IOException()|};
            }
            """;
        const string fixedSource = """
            using Darp.Results;
            using IOException = global::System.IO.IOException;
            using System = MyCompany.System;

            namespace MyCompany.System
            {
                sealed class Placeholder { }
            }

            static class TestClass
            {
                /// <exception cref="global::System.IO.IOException"></exception>
                static Result<int, string> Run() => throw new IOException();
            }
            """;

        await VerifyAsync(source, fixedSource);
    }

    private static Task VerifyAsync(string source, string fixedSource)
    {
        var test = new CSharpCodeFixTest<
            KnownExceptionMayEscapeAnalyzer,
            DocumentEscapingExceptionsCodeFixer,
            DefaultVerifier
        >
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
        };
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Result<,>).Assembly.Location));
        return test.RunAsync(CancellationToken.None);
    }
}

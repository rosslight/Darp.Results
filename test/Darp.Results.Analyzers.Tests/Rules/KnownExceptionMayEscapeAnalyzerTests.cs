using Darp.Results.Analyzers.Rules;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Darp.Results.Analyzers.Tests.Rules;

public sealed class KnownExceptionMayEscapeAnalyzerTests
{
    [Fact]
    public void HelpLink_ShouldBeCorrect()
    {
        ResultHelpers.VerifyHelpLink<KnownExceptionMayEscapeAnalyzer>("DR0004");
    }

    [Fact]
    public async Task ExplicitThrow_InResultReturningMethod_ShouldWarn()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            static class TestClass
            {
                static Result<int, string> Run() => {|DR0004:throw new IOException()|};
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ExplicitThrow_OutsideResultReturningMethod_ShouldNotWarn()
    {
        const string source = """
            using System.IO;

            static class TestClass
            {
                static int Run() => throw new IOException();
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task DefaultAllowedExceptionTypes_ShouldNotWarn()
    {
        const string source = """
            using Darp.Results;
            using System;
            using System.Diagnostics;
            using System.Runtime.CompilerServices;

            static class TestClass
            {
                static Result<int, string> Argument() => throw new ArgumentNullException();
                static Result<int, string> Cancellation() => throw new OperationCanceledException();
                static Result<int, string> NotImplemented() => throw new NotImplementedException();
                static Result<int, string> NotSupported() => throw new NotSupportedException();
                static Result<int, string> Unreachable() => throw new UnreachableException();
                static Result<int, string> ObjectDisposed() => throw new ObjectDisposedException("resource");
                static Result<int, string> SwitchExpression() => throw new SwitchExpressionException();
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ExplicitThrow_InTaskAndValueTaskResultMethods_ShouldWarn()
    {
        const string source = """
            using Darp.Results;
            using System.IO;
            using System.Threading.Tasks;

            static class TestClass
            {
                static Task<Result<int, string>> RunTask() => {|DR0004:throw new IOException()|};
                static ValueTask<Result<int, string>> RunValueTask() => {|DR0004:throw new IOException()|};
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ExplicitThrow_InResultReturningLocalFunctionAndLambda_ShouldWarn()
    {
        const string source = """
            using Darp.Results;
            using System;
            using System.IO;

            static class TestClass
            {
                static void Configure()
                {
                    Result<int, string> Local() => {|DR0004:throw new IOException()|};
                    Func<Result<int, string>> lambda = () => {|DR0004:throw new IOException()|};
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ExceptionDeclaredOnResultMethod_ShouldNotWarn()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            static class TestClass
            {
                /// <exception cref="IOException">The input cannot be read.</exception>
                static Result<int, string> Run() => throw new IOException();
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task DocumentedExceptionFromInvocation_ShouldWarnOnceForAllExceptionTypes()
    {
        const string source = """
            using Darp.Results;
            using System;
            using System.IO;

            static class Dependency
            {
                /// <exception cref="IOException">The input cannot be read.</exception>
                /// <exception cref="UnauthorizedAccessException">Access is denied.</exception>
                /// <exception cref="ArgumentException">The path is invalid.</exception>
                internal static int Read(string path) => 0;
            }

            static class TestClass
            {
                static Result<int, string> Run(string path) => {|DR0004:Dependency.Read(path)|};
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task DocumentedExceptionDeclaredByCallingMethod_ShouldNotWarn()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            static class Dependency
            {
                /// <exception cref="IOException">The input cannot be read.</exception>
                internal static int Read() => 0;
            }

            static class TestClass
            {
                /// <exception cref="IOException">The input cannot be read.</exception>
                static Result<int, string> Run() => Dependency.Read();
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task NonExceptionTypeInExceptionElement_ShouldBeIgnored()
    {
        const string source = """
            using Darp.Results;

            static class Dependency
            {
                /// <exception cref="string">This is not a valid exception contract.</exception>
                internal static int Read() => 0;
            }

            static class TestClass
            {
                static Result<int, string> Run() => Dependency.Read();
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task CaughtException_ShouldNotWarn()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            static class Dependency
            {
                /// <exception cref="IOException">The input cannot be read.</exception>
                internal static int Read() => 0;
            }

            static class TestClass
            {
                static Result<int, string> Run()
                {
                    try
                    {
                        return Dependency.Read();
                    }
                    catch (IOException)
                    {
                        return "read error";
                    }
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task FilteredCatch_ShouldNotBeAssumedToHandleException()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            static class TestClass
            {
                static Result<int, string> Run(bool handle)
                {
                    try
                    {
                        {|DR0004:throw new IOException();|}
                    }
                    catch (IOException) when (handle)
                    {
                        return "read error";
                    }
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task Rethrow_ShouldWarn()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            static class TestClass
            {
                static Result<int, string> Run()
                {
                    try
                    {
                        throw new IOException();
                    }
                    catch (IOException)
                    {
                        {|DR0004:throw;|}
                    }
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task DocumentedConstructorAndPropertyExceptions_ShouldWarn()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            sealed class Dependency
            {
                /// <exception cref="IOException">Construction failed.</exception>
                internal Dependency() { }

                /// <exception cref="IOException">Reading failed.</exception>
                internal int Value => 0;
            }

            static class TestClass
            {
                static Result<Dependency, string> Create() => {|DR0004:new Dependency()|};
                static Result<int, string> Read(Dependency dependency) => {|DR0004:dependency.Value|};
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task TaskCreatedInsideTryButAwaitedOutside_ShouldWarn()
    {
        const string source = """
            using Darp.Results;
            using System.IO;
            using System.Threading.Tasks;

            static class Dependency
            {
                /// <exception cref="IOException">Reading failed.</exception>
                internal static Task<int> ReadAsync() => Task.FromResult(0);
            }

            static class TestClass
            {
                static async Task<Result<int, string>> Run()
                {
                    Task<int> task;
                    try
                    {
                        task = {|DR0004:Dependency.ReadAsync()|};
                    }
                    catch (IOException)
                    {
                        return "read error";
                    }

                    return await task;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task TaskDirectlyAwaitedInsideTry_ShouldNotWarn()
    {
        const string source = """
            using Darp.Results;
            using System.IO;
            using System.Threading.Tasks;

            static class Dependency
            {
                /// <exception cref="IOException">Reading failed.</exception>
                internal static Task<int> ReadAsync() => Task.FromResult(0);
            }

            static class TestClass
            {
                static async Task<Result<int, string>> Run()
                {
                    try
                    {
                        return await Dependency.ReadAsync();
                    }
                    catch (IOException)
                    {
                        return "read error";
                    }
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task TaskDirectlyAwaitedWithConfigureAwaitInsideTry_ShouldNotWarn()
    {
        const string source = """
            using Darp.Results;
            using System.IO;
            using System.Threading.Tasks;

            static class Dependency
            {
                /// <exception cref="IOException">Reading failed.</exception>
                internal static Task<int> ReadAsync() => Task.FromResult(0);
            }

            static class TestClass
            {
                static async Task<Result<int, string>> Run()
                {
                    try
                    {
                        return await Dependency.ReadAsync().ConfigureAwait(false);
                    }
                    catch (IOException)
                    {
                        return "read error";
                    }
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ExceptionFromReferencedAssemblyDocumentationAdditionalFile_ShouldWarn()
    {
        const string source = """
            using Darp.Results;

            static class TestClass
            {
                static Result<int, string> Run() => {|DR0004:new Result.Ok<int, string>(1)|};
            }
            """;
        const string documentation = """
            <?xml version="1.0"?>
            <doc>
              <assembly>
                <name>Darp.Results</name>
              </assembly>
              <members>
                <member name="M:Darp.Results.Result.Ok`2.#ctor(`0)">
                  <exception cref="T:System.IO.IOException">Creation failed.</exception>
                </member>
              </members>
            </doc>
            """;

        var test = new ResultAnalyzerTest<KnownExceptionMayEscapeAnalyzer> { TestCode = source };
        test.TestState.AdditionalFiles.Add(
            (Path.ChangeExtension(typeof(Result<,>).Assembly.Location, ".xml"), documentation)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExceptionFromReferencedMethodDocumentationAdditionalFile_ShouldWarn()
    {
        const string source = """
            using Darp.Results;

            static class TestClass
            {
                static Result<int, string> Run() => {|DR0004:Result.Try(() => 1)|}.MapError(_ => "error");
            }
            """;
        const string documentation = """
            <?xml version="1.0"?>
            <doc>
              <assembly>
                <name>Darp.Results</name>
              </assembly>
              <members>
                <member name="M:Darp.Results.Result.Try``1(System.Func{``0})">
                  <exception cref="T:System.IO.IOException">Execution failed.</exception>
                </member>
              </members>
            </doc>
            """;

        var test = new ResultAnalyzerTest<KnownExceptionMayEscapeAnalyzer> { TestCode = source };
        test.TestState.AdditionalFiles.Add(
            (Path.ChangeExtension(typeof(Result<,>).Assembly.Location, ".xml"), documentation)
        );
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExceptionFromReferencedGenericExtensionDocumentationAdditionalFile_ShouldWarn()
    {
        const string dependencySource = """
            namespace Dependency;

            public sealed class Values<T> { }

            public static class Extensions
            {
                public static T First<T>(this Values<T> values) => default(T);
            }
            """;
        const string source = """
            using Darp.Results;
            using Dependency;

            static class TestClass
            {
                static Result<int, string> Run(Values<int> values) => {|DR0004:values.First()|};
            }
            """;
        const string documentation = """
            <?xml version="1.0"?>
            <doc>
              <assembly>
                <name>Dependency</name>
              </assembly>
              <members>
                <member name="M:Dependency.Extensions.First``1(Dependency.Values{``0})">
                  <exception cref="T:System.InvalidOperationException">The sequence is empty.</exception>
                </member>
              </members>
            </doc>
            """;

        var test = new ResultAnalyzerTest<KnownExceptionMayEscapeAnalyzer> { TestCode = source };
        test.TestState.AdditionalProjects["Dependency"].Sources.Add(dependencySource);
        test.TestState.AdditionalProjectReferences.Add("Dependency");
        test.TestState.AdditionalFiles.Add(("Dependency.xml", documentation));
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ImplicitInterfaceException_ShouldPermitImplementationThrow()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            interface IReader
            {
                /// <exception cref="IOException"></exception>
                Result<int, string> Read();
            }

            sealed class Reader : IReader
            {
                public Result<int, string> Read() => throw new IOException();
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ImplicitInterfaceException_ShouldWarnThroughConcreteMethod()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            interface IReader
            {
                /// <exception cref="IOException"></exception>
                Result<int, string> Read();
            }

            sealed class Reader : IReader
            {
                public Result<int, string> Read() => 0;
            }

            static class TestClass
            {
                static Result<int, string> Run(Reader reader) => {|DR0004:reader.Read()|};
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ImplicitInterfaceException_ShouldWarnThroughConcreteProperty()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            interface IReader
            {
                /// <exception cref="IOException"></exception>
                Result<int, string> Value { get; }
            }

            sealed class Reader : IReader
            {
                public Result<int, string> Value => 0;
            }

            static class TestClass
            {
                static Result<int, string> Run(Reader reader) => {|DR0004:reader.Value|};
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ConfiguredSystemException_ShouldAllowEveryException()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            static class TestClass
            {
                static Result<int, string> Run() => throw new IOException();
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            dotnet_diagnostic.DR0004.allowed_exception_types = System.Exception
            """;

        await VerifyAsync(source, editorConfig);
    }

    [Fact]
    public async Task ConfiguredAllowedExceptions_ShouldReplaceDefaults()
    {
        const string source = """
            using Darp.Results;
            using System;

            static class TestClass
            {
                static Result<int, string> Run() => {|DR0004:throw new ArgumentException()|};
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            dotnet_diagnostic.DR0004.allowed_exception_types = System.IO.IOException
            """;

        await VerifyAsync(source, editorConfig);
    }

    [Fact]
    public async Task SeverityNone_ShouldDisableRule()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            static class TestClass
            {
                static Result<int, string> Run() => throw new IOException();
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            dotnet_diagnostic.DR0004.severity = none
            """;

        await VerifyAsync(source, editorConfig);
    }

    private static Task VerifyAsync(string source, string? editorConfig = null)
    {
        var test = new ResultAnalyzerTest<KnownExceptionMayEscapeAnalyzer> { TestCode = source };
        if (editorConfig is not null)
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
        return test.RunAsync(CancellationToken.None);
    }
}

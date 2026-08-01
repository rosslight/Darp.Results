using Darp.Results.Analyzers.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Darp.Results.Analyzers.Tests.Rules;

public sealed class KnownExceptionMayEscapeAnalyzerTests
{
    [Fact]
    public void HelpLink_ShouldBeCorrect()
    {
        ResultHelpers.VerifyHelpLink<KnownExceptionMayEscapeAnalyzer>("DR0004");
        ResultHelpers.VerifyHelpLink<DocumentedExceptionMayEscapeAnalyzer>("DR0005");
    }

    [Fact]
    public async Task SplitAnalyzers_ShouldIgnoreTheOtherEvidenceSource()
    {
        const string explicitThrow = """
            using Darp.Results;
            using System.IO;

            static class TestClass
            {
                static Result<int, string> Run() => throw new IOException();
            }
            """;
        const string documentedInvocation = """
            using Darp.Results;
            using System.IO;

            static class Dependency
            {
                /// <exception cref="IOException"></exception>
                internal static int Read() => 0;
            }

            static class TestClass
            {
                static Result<int, string> Run() => Dependency.Read();
            }
            """;

        await VerifyDocumentedAsync(explicitThrow);
        await VerifyAsync(documentedInvocation);
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
            using System.Threading;

            static class TestClass
            {
                static Result<int, string> Argument() => throw new ArgumentNullException();
                static Result<int, string> Cancellation() => throw new OperationCanceledException();
                static Result<int, string> NotImplemented() => throw new NotImplementedException();
                static Result<int, string> NotSupported() => throw new NotSupportedException();
                static Result<int, string> Unreachable() => throw new UnreachableException();
                static Result<int, string> ObjectDisposed() => throw new ObjectDisposedException("resource");
                static Result<int, string> SwitchExpression() => throw new SwitchExpressionException();
                static Result<int, string> SemaphoreFull() => throw new SemaphoreFullException();
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
                static Result<int, string> Run(string path) => {|DR0005:Dependency.Read(path)|};
            }
            """;

        await VerifyDocumentedAsync(source);
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

        await VerifyDocumentedAsync(source);
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

        await VerifyDocumentedAsync(source);
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

        await VerifyDocumentedAsync(source);
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
    public async Task ConstantTrueFilteredCatch_ShouldHandleException()
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
                    catch (IOException) when (true)
                    {
                        return "read error";
                    }
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task CatchVariableSingleTypeFilter_ShouldHandleMatchingException()
    {
        const string source = """
            using Darp.Results;
            using System;

            static class TestClass
            {
                static Result<int, string> Run()
                {
                    try
                    {
                        throw new InvalidOperationException();
                    }
                    catch (Exception exception) when (exception is InvalidOperationException)
                    {
                        return "invalid operation";
                    }
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task CatchVariableOrTypePattern_ShouldHandleMatchingExceptions()
    {
        const string source = """
            using Darp.Results;
            using System;
            using System.IO;

            static class TestClass
            {
                static Result<int, string> Read()
                {
                    try
                    {
                        throw new IOException();
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        return "read error";
                    }
                }

                static Result<int, string> Access()
                {
                    try
                    {
                        throw new UnauthorizedAccessException();
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        return "access error";
                    }
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task CatchVariableOrTypePattern_ShouldWarnForUnmatchedException()
    {
        const string source = """
            using Darp.Results;
            using System;
            using System.IO;

            static class TestClass
            {
                static Result<int, string> Run()
                {
                    try
                    {
                        {|DR0004:throw new InvalidOperationException();|}
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        return "handled";
                    }
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task TypePatternOnOtherVariable_ShouldNotHandleException()
    {
        const string source = """
            using Darp.Results;
            using System;
            using System.IO;

            static class TestClass
            {
                static Result<int, string> Run(Exception other)
                {
                    try
                    {
                        {|DR0004:throw new IOException();|}
                    }
                    catch (Exception ex) when (other is IOException or UnauthorizedAccessException)
                    {
                        return ex.Message;
                    }
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task CatchVariableOrTypePattern_ShouldHandleDocumentedExceptions()
    {
        const string source = """
            using Darp.Results;
            using System;
            using System.IO;

            static class Dependency
            {
                /// <exception cref="IOException">The input cannot be read.</exception>
                /// <exception cref="UnauthorizedAccessException">Access was denied.</exception>
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
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        return "read error";
                    }
                }
            }
            """;

        await VerifyDocumentedAsync(source);
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
                static Result<Dependency, string> Create() => {|DR0005:new Dependency()|};
                static Result<int, string> Read(Dependency dependency) => {|DR0005:dependency.Value|};
            }
            """;

        await VerifyDocumentedAsync(source);
    }

    [Fact]
    public async Task DocumentedPropertyInsideNameof_ShouldNotWarn()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            sealed class Dependency
            {
                /// <exception cref="IOException">Reading failed.</exception>
                internal int Value => 0;
            }

            static class TestClass
            {
                static Result<string, int> Run(Dependency dependency) => nameof(dependency.Value);
            }
            """;

        await VerifyDocumentedAsync(source);
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
                        task = {|DR0005:Dependency.ReadAsync()|};
                    }
                    catch (IOException)
                    {
                        return "read error";
                    }

                    return await task;
                }
            }
            """;

        await VerifyDocumentedAsync(source);
    }

    [Fact]
    public async Task TaskValuedPropertyReadInsideTryButAwaitedOutside_ShouldWarn()
    {
        const string source = """
            using Darp.Results;
            using System.IO;
            using System.Threading.Tasks;

            static class Dependency
            {
                /// <exception cref="IOException">Reading failed.</exception>
                internal static Task<int> Value => Task.FromResult(0);
            }

            static class TestClass
            {
                static async Task<Result<int, string>> RunOutside()
                {
                    Task<int> task;
                    try
                    {
                        task = {|DR0005:Dependency.Value|};
                    }
                    catch (IOException)
                    {
                        return "read error";
                    }

                    return await task;
                }

                static async Task<Result<int, string>> RunInside()
                {
                    try
                    {
                        return await Dependency.Value;
                    }
                    catch (IOException)
                    {
                        return "read error";
                    }
                }
            }
            """;

        await VerifyDocumentedAsync(source);
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

        await VerifyDocumentedAsync(source);
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

        await VerifyDocumentedAsync(source);
    }

    [Fact]
    public async Task ExceptionFromReferencedAssemblyDocumentationAdditionalFile_ShouldWarn()
    {
        const string source = """
            using Darp.Results;

            static class TestClass
            {
                static Result<int, string> Run() => {|DR0005:new Result.Ok<int, string>(1)|};
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

        var test = new ResultAnalyzerTest<DocumentedExceptionMayEscapeAnalyzer> { TestCode = source };
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
                static Result<int, string> Run() => {|DR0005:Result.Try(() => 1)|}.MapError(_ => "error");
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

        var test = new ResultAnalyzerTest<DocumentedExceptionMayEscapeAnalyzer> { TestCode = source };
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
                static Result<int, string> Run(Values<int> values) => {|DR0005:values.First()|};
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

        var test = new ResultAnalyzerTest<DocumentedExceptionMayEscapeAnalyzer> { TestCode = source };
        test.TestState.AdditionalProjects["Dependency"].Sources.Add(dependencySource);
        test.TestState.AdditionalProjectReferences.Add("Dependency");
        test.TestState.AdditionalFiles.Add(("Dependency.xml", documentation));
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExceptionFromReferencedGenericInterfaceDocumentationAdditionalFile_ShouldWarn()
    {
        const string dependencySource = """
            namespace Dependency;

            public interface IReader<T>
            {
                T Read();
            }
            """;
        const string source = """
            using Darp.Results;
            using Dependency;

            sealed class Reader : IReader<int>
            {
                public int Read() => 0;
            }

            static class TestClass
            {
                static Result<int, string> Run(Reader reader) => {|DR0005:reader.Read()|};
            }
            """;
        const string documentation = """
            <?xml version="1.0"?>
            <doc>
              <assembly>
                <name>Dependency</name>
              </assembly>
              <members>
                <member name="M:Dependency.IReader`1.Read">
                  <exception cref="T:System.InvalidOperationException"></exception>
                </member>
              </members>
            </doc>
            """;

        var test = new ResultAnalyzerTest<DocumentedExceptionMayEscapeAnalyzer> { TestCode = source };
        test.TestState.AdditionalProjects["Dependency"].Sources.Add(dependencySource);
        test.TestState.AdditionalProjectReferences.Add("Dependency");
        test.TestState.AdditionalFiles.Add(("Dependency.xml", documentation));
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ReferencedGenericOverrideAndExplicitInterfaceContracts_ShouldResolveDefinitions()
    {
        const string dependencySource = """
            using Darp.Results;

            namespace Dependency;

            public abstract class Base<T>
            {
                public abstract T Read();
            }

            public interface IReader<T>
            {
                Result<T, string> ReadResult();
            }
            """;
        const string source = """
            using Darp.Results;
            using Dependency;
            using System.IO;

            sealed class Reader : Base<int>
            {
                public override int Read() => 0;
            }

            sealed class ExplicitReader : IReader<int>
            {
                Result<int, string> IReader<int>.ReadResult() => throw new IOException();
            }

            static class TestClass
            {
                static Result<int, string> Run(Reader reader) => {|DR0005:reader.Read()|};
            }
            """;
        const string documentation = """
            <doc>
              <members>
                <member name="M:Dependency.Base`1.Read">
                  <exception cref="T:System.InvalidOperationException"></exception>
                </member>
                <member name="M:Dependency.IReader`1.ReadResult">
                  <exception cref="T:System.IO.IOException"></exception>
                </member>
              </members>
            </doc>
            """;

        var test = new ResultAnalyzerTest<DocumentedExceptionMayEscapeAnalyzer> { TestCode = source };
        var dependency = test.TestState.AdditionalProjects["Dependency"];
        dependency.Sources.Add(dependencySource);
        dependency.AdditionalReferences.Add(
            MetadataReference.CreateFromFile(typeof(global::Darp.Results.Result<,>).Assembly.Location)
        );
        test.TestState.AdditionalProjectReferences.Add("Dependency");
        test.TestState.AdditionalFiles.Add(("Dependency.xml", documentation));
        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ImplicitInterfaceContracts_ShouldUseMethodDefinitionsAndReceiverType()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            interface IGenericReader
            {
                /// <exception cref="IOException"></exception>
                T Read<T>();
            }

            sealed class GenericReader : IGenericReader
            {
                public T Read<T>() => default(T);
            }

            interface IReader
            {
                /// <exception cref="IOException"></exception>
                int Read();
            }

            class ReaderBase
            {
                public int Read() => 0;
            }

            sealed class Reader : ReaderBase, IReader { }

            static class TestClass
            {
                static Result<int, string> RunGeneric(GenericReader reader) => {|DR0005:reader.Read<int>()|};

                static Result<int, string> RunInherited(Reader reader) => {|DR0005:reader.Read()|};
            }
            """;

        await VerifyDocumentedAsync(source);
    }

    [Fact]
    public async Task DocumentedUserDefinedOperators_ShouldWarn()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            readonly struct Number
            {
                /// <exception cref="IOException"></exception>
                public static Number operator +(Number left, Number right) => left;

                /// <exception cref="IOException"></exception>
                public static Number operator -(Number value) => value;

                /// <exception cref="IOException"></exception>
                public static Number operator ++(Number value) => value;

                /// <exception cref="IOException"></exception>
                public static explicit operator int(Number value) => 0;

                /// <exception cref="IOException"></exception>
                public static implicit operator string(Number value) => string.Empty;
            }

            static class TestClass
            {
                static Result<int, string> Calculate(Number left, Number right)
                {
                    left = {|DR0005:left + right|};
                    left = {|DR0005:-left|};
                    {|DR0005:left += right|};
                    {|DR0005:left++|};
                    string text = {|DR0005:left|};
                    return {|DR0005:(int)left|};
                }
            }
            """;

        await VerifyDocumentedAsync(source);
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
                static Result<int, string> Run(Reader reader) => {|DR0005:reader.Read()|};
            }
            """;

        await VerifyDocumentedAsync(source);
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
                static Result<int, string> Run(Reader reader) => {|DR0005:reader.Value|};
            }
            """;

        await VerifyDocumentedAsync(source);
    }

    [Fact]
    public async Task ConfiguredExcludedMembers_ShouldExcludeEveryListedMember()
    {
        const string source = """
            using Darp.Results;
            using System.IO;

            namespace Test;

            static class Dependency
            {
                /// <exception cref="IOException"></exception>
                internal static int Read() => 0;

                /// <exception cref="IOException"></exception>
                internal static int Value => 0;

                /// <exception cref="IOException"></exception>
                internal static int Other() => 0;
            }

            static class TestClass
            {
                static Result<int, string> Run()
                {
                    _ = Dependency.Read();
                    _ = Dependency.Value;
                    return {|DR0005:Dependency.Other()|};
                }
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            dotnet_code_quality.DR0005.excluded_members = P:Test.Dependency.Value|M:Test.Dependency.Read
            """;

        await VerifyDocumentedAsync(source, editorConfig);
    }

    [Fact]
    public async Task SharedAllowedExceptions_ShouldConfigureDocumentedAnalyzer()
    {
        const string source = """
            using Darp.Results;
            using System;
            using System.IO;

            static class Dependency
            {
                /// <exception cref="IOException"></exception>
                /// <exception cref="ArgumentException"></exception>
                internal static int Read() => 0;
            }

            static class TestClass
            {
                static Result<int, string> Run() => {|DR0005:Dependency.Read()|};
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            dotnet_code_quality.darp_results_allowed_exception_types = System.IO.IOException
            """;

        await VerifyDocumentedAsync(source, editorConfig);
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
            dotnet_code_quality.darp_results_allowed_exception_types = System.Exception
            """;

        await VerifyAsync(source, editorConfig);
    }

    [Fact]
    public async Task ConfiguredNestedException_ShouldUseCSharpTypeName()
    {
        const string source = """
            using Darp.Results;
            using System;

            namespace MyCompany;

            static class Errors
            {
                internal sealed class ExpectedException : Exception { }
            }

            static class TestClass
            {
                static Result<int, string> Run() => throw new Errors.ExpectedException();
            }
            """;
        const string editorConfig = """
            root = true

            [*.cs]
            dotnet_code_quality.darp_results_allowed_exception_types = MyCompany.Errors.ExpectedException
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
            dotnet_code_quality.darp_results_allowed_exception_types = System.IO.IOException
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

    private static Task VerifyDocumentedAsync(string source, string? editorConfig = null)
    {
        var test = new ResultAnalyzerTest<DocumentedExceptionMayEscapeAnalyzer> { TestCode = source };
        if (editorConfig is not null)
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
        return test.RunAsync(CancellationToken.None);
    }
}

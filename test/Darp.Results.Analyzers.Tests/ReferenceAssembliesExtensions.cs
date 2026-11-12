using Microsoft.CodeAnalysis.Testing;

namespace Darp.Results.Analyzers.Tests;

static class ReferenceAssembliesExtensions
{
    static readonly Lazy<ReferenceAssemblies> lazyNet110 = new(() =>
        new(
            targetFramework: "net11.0",
            referenceAssemblyPackage: new PackageIdentity("Microsoft.NETCore.App.Ref", "11.0.0"),
            referenceAssemblyPath: Path.Combine("ref", "net11.0")
        )
    );

    extension(ReferenceAssemblies.Net)
    {
        /// <summary> Workaround until https://github.com/dotnet/roslyn-sdk/issues/1233 is resolved </summary>
        public static ReferenceAssemblies Net110 => lazyNet110.Value;
    }
}

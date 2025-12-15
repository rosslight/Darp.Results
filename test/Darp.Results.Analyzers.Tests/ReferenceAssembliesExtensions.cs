using Microsoft.CodeAnalysis.Testing;

namespace Darp.Results.Analyzers.Tests;

static class ReferenceAssembliesExtensions
{
    static readonly Lazy<ReferenceAssemblies> lazyNet100 = new(() =>
        new(
            targetFramework: "net10.0",
            referenceAssemblyPackage: new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0-rc.2.25502.107"),
            referenceAssemblyPath: Path.Combine("ref", "net10.0")
        )
    );

    extension(ReferenceAssemblies.Net)
    {
        /// <summary> Workaround until https://github.com/dotnet/roslyn-sdk/issues/1233 is resolved </summary>
        public static ReferenceAssemblies Net100 => lazyNet100.Value;
    }
}

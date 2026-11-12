# Darp.Results.Analyzers

Roslyn analyzers and code fixers for the Darp.Results library to help developers use Result types correctly and catch common mistakes at compile time.

## Overview

The Darp.Results.Analyzers package provides static analysis tools that integrate with Visual Studio, VS Code, and other IDEs to:

- Detect unused `Result<TValue, TError>` return values
- Detect ignored awaited expressions that produce a `Result<TValue, TError>`
- Detect explicit and XML-documented exceptions that can escape Result-returning functions
- Warn when a project consumes Darp.Results with a language version below C# 15

## Installation

Add the analyzer package to your project:

```xml
<PackageReference Include="Darp.Results.Analyzers" Version="1.4.0" />
```

The analyzers will automatically activate in your IDE and during build.

## Rules

| Rule ID                  | Title                         | Severity | Description                                           |
|--------------------------|-------------------------------|----------|-------------------------------------------------------|
| [DR0001](docs/DR0001.md) | Use return value              | Warning  | Result return values should be checked                |
| [DR0004](docs/DR0004.md) | Explicit exception may escape | Warning  | Explicit exceptions should use the Result error channel |
| [DR0005](docs/DR0005.md) | Documented exception may escape | Warning | XML-documented exceptions should use the Result error channel |
| [DR0006](docs/DR0006.md) | C# 15 required | Warning | Result is a C# 15 union |

Switch exhaustiveness over `Ok` and `Err` is checked by the C# 15 compiler (CS8509).

## .editorconfig - default values

```ini
# DR0001: Use return value
dotnet_diagnostic.DR0001.severity = warning

# DR0004: Explicit exception may escape
dotnet_diagnostic.DR0004.severity = warning

# DR0005: Documented exception may escape
dotnet_diagnostic.DR0005.severity = warning

# DR0006: C# 15 required
dotnet_diagnostic.DR0006.severity = warning
```

DR0004 and DR0005 are disabled by default in projects where `IsTestProject` is `true`. To analyze exception handling in test projects, opt in through MSBuild:

```xml
<PropertyGroup>
  <DarpResultsAnalyzeTestProjects>true</DarpResultsAnalyzeTestProjects>
</PropertyGroup>
```

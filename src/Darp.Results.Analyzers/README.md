# Darp.Results.Analyzers

Roslyn analyzers and code fixers for the Darp.Results library to help developers use Result types correctly and catch common mistakes at compile time.

## Overview

The Darp.Results.Analyzers package provides static analysis tools that integrate with Visual Studio, VS Code, and other IDEs to:

- Detect unused `Result<TValue, TError>` return values
- Detect ignored awaited expressions that produce a `Result<TValue, TError>`
- Require switch expressions on results to cover both `Ok` and `Err` cases
- Suppress redundant compiler exhaustiveness warnings for result switch expressions

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
| [DR0002](docs/DR0002.md) | Switch expression missing arm | Error    | Switch expressions on Result should handle both cases |

## Suppressors

| ID     | Suppressed diagnostic | Description                                      |
|--------|-----------------------|--------------------------------------------------|
| DR0003 | CS8509                | Result switch exhaustiveness is covered by DR0002 |

## .editorconfig - default values

```ini
# DR0001: Use return value
dotnet_diagnostic.DR0001.severity = warning

# DR0002: Switch expression missing arm
dotnet_diagnostic.DR0002.severity = error
```

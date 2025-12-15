# Changelog

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Content is taken from conventional commits and aggregated in a [release-please](https://github.com/googleapis/release-please) PR.

## [1.2.0](https://github.com/rosslight/Darp.Results/compare/v1.1.1...v1.2.0) (2025-09-25)

### Features
- Add `UnwrapOrDefault` ([3f94103](https://github.com/rosslight/Darp.Results/commit/3f94103))

## [1.1.1](https://github.com/rosslight/Darp.Results/compare/v1.1.0...v1.1.1) (2025-09-08)

### Bug Fixes
- Analyzers should warn when async methods are unused ([37ae846](https://github.com/rosslight/Darp.Results/commit/37ae846))

## [1.1.0](https://github.com/rosslight/Darp.Results/compare/v1.0.0...v1.1.0) (2025-09-08)

### Bug Fixes
- Adjust analyzers to new `Result` structure ([c64c787](https://github.com/rosslight/Darp.Results/commit/c64c787))

### Features
- Move `Ok` and `Err` to static class ([8156647](https://github.com/rosslight/Darp.Results/commit/8156647))

## [1.0.0](https://github.com/rosslight/Darp.Results/releases/tag/v1.0.0) (2025-09-05)

### Bug Fixes
- Discards should count as used ([a9f1b38](https://github.com/rosslight/Darp.Results/commit/a9f1b38))
- Fix order of the fixed switch arms ([fbfe197](https://github.com/rosslight/Darp.Results/commit/fbfe197))

### Features
- Add code fixer to insert missing switch arms ([0352c4c](https://github.com/rosslight/Darp.Results/commit/0352c4c))
- Deconstruct result cases for cleaner switch expressions ([8c0c60b](https://github.com/rosslight/Darp.Results/commit/8c0c60b))
- Equality based on `Error` / `Value` ([d8b680d](https://github.com/rosslight/Darp.Results/commit/d8b680d))
- Suppress CS8509 for result types ([2d6e078](https://github.com/rosslight/Darp.Results/commit/2d6e078))

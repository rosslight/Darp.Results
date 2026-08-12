# Changelog

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Content is taken from conventional commits and aggregated in a [release-please](https://github.com/googleapis/release-please) PR.

## [1.9.1](https://github.com/rosslight/Darp.Results/compare/v1.9.0...v1.9.1) (2026-08-12)


### Bug Fixes

* analyzer xml doc inputs do not leak to other analyzers ([#30](https://github.com/rosslight/Darp.Results/issues/30)) ([c59fd5e](https://github.com/rosslight/Darp.Results/commit/c59fd5e7890ccf1d00bb18c0379a6c0bc48a2a6e))

## [1.9.0](https://github.com/rosslight/Darp.Results/compare/v1.8.0...v1.9.0) (2026-08-01)


### Features

* **analyzers:** disable exception warnings in tests ([2e85f18](https://github.com/rosslight/Darp.Results/commit/2e85f186b8f3c049891dde1a38293743ca9c8a3b))


### Bug Fixes

* **analyzers:** preserve concurrent member exclusions ([2e3e033](https://github.com/rosslight/Darp.Results/commit/2e3e033384b294ea8d1c4425c9d601a88593fc3d))

## [1.8.0](https://github.com/rosslight/Darp.Results/compare/v1.7.0...v1.8.0) (2026-08-01)


### Features

* **analyzers:** add DR0005 exclusion code fix ([03a299a](https://github.com/rosslight/Darp.Results/commit/03a299a490b3a213ea497434e15b17c63ab63778))


### Bug Fixes

* **analyzers:** recognize exception type filters ([7b0428b](https://github.com/rosslight/Darp.Results/commit/7b0428ba4ed8e93fefda6f42646dec2e5f630d32))
* **analyzers:** recognize single-type catch filters ([d4c2e0c](https://github.com/rosslight/Darp.Results/commit/d4c2e0c9f35f4d1948befd8a4bcfd162271aa910))

## [1.7.0](https://github.com/rosslight/Darp.Results/compare/v1.6.0...v1.7.0) (2026-08-01)


### Features

* **analyzers:** split documented exception diagnostics ([ef9d653](https://github.com/rosslight/Darp.Results/commit/ef9d653c3d23269dbcda216ea3b35efa45dd3691))


### Bug Fixes

* **analyzers:** allow semaphore full exceptions ([c555a0c](https://github.com/rosslight/Darp.Results/commit/c555a0c292999ea697fee67921b14beae9c9b4ff))

## [1.6.0](https://github.com/rosslight/Darp.Results/compare/v1.5.0...v1.6.0) (2026-08-01)


### Features

* warn when known exceptions escape results ([#25](https://github.com/rosslight/Darp.Results/issues/25)) ([9126319](https://github.com/rosslight/Darp.Results/commit/912631911e42cfa61f1831251571be50ca2b121f))

## [1.5.0](https://github.com/rosslight/Darp.Results/compare/v1.4.1...v1.5.0) (2026-07-31)


### Features

* **shouldly:** add ValueTask result assertions ([#22](https://github.com/rosslight/Darp.Results/issues/22)) ([70c9ef1](https://github.com/rosslight/Darp.Results/commit/70c9ef1462022b9d043573e481fdb66eba456ffd))


### Bug Fixes

* add grouping to dependabot ([3e45474](https://github.com/rosslight/Darp.Results/commit/3e454746f46cd2cf3e7e27228c4aee4c09add0d6))
* **release:** recover 1.5.0 publishing ([#23](https://github.com/rosslight/Darp.Results/issues/23)) ([ce265ec](https://github.com/rosslight/Darp.Results/commit/ce265eca9fb5129eedfc5691d730841daa329728))

## [1.4.1](https://github.com/rosslight/Darp.Results/compare/v1.4.0...v1.4.1) (2026-05-25)


### Bug Fixes

* HelpUri now points to our repository ([d754969](https://github.com/rosslight/Darp.Results/commit/d7549699771bf4eac625cfd69ab5ebb8018e5c70))

## [1.4.0](https://github.com/rosslight/Darp.Results/compare/v1.3.0...v1.4.0) (2026-05-15)


### Features

* Improve error messages by adding tostring unexpected ([847ede5](https://github.com/rosslight/Darp.Results/commit/847ede528dc268aff75128ed57adb4d9676cabf2))


### Bug Fixes

* Adding Shouldy extensions that work on tasks ([#3](https://github.com/rosslight/Darp.Results/issues/3)) ([5b0eec7](https://github.com/rosslight/Darp.Results/commit/5b0eec77cf0f4ddb9337198ac9415d423cb4408d))
* Warn when any awaited result is unused ([#2](https://github.com/rosslight/Darp.Results/issues/2)) ([f235573](https://github.com/rosslight/Darp.Results/commit/f23557338b698971dc97b7cd2ed395de55943dba))

## [1.3.0](https://github.com/rosslight/Darp.Results/compare/v1.2.0...v1.3.0) (2025-12-15)


### Features

* Add task based extension methods for Map, And, Or, ... ([7c29269](https://github.com/rosslight/Darp.Results/commit/7c2926936e67ed858f0dd2c0f6d9804ea9af9969))
* Support net9.0 and net10.0 ([36929a9](https://github.com/rosslight/Darp.Results/commit/36929a9118572c245cf2e5df0f167ba437ae7407))

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

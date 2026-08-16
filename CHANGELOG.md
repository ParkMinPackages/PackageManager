# Changelog
All notable changes to this package will be documented in this file.

## [5.1.0] - 2026-08-16

### Added
- Added Git and NuGet dependency discovery through repository `parkmin-dependencies.json` files.
- Added dependency visibility controls and installed, missing, and version-mismatch status indicators.
- Added ScriptableObject-based public Git package catalog entries and an Inspector creation workflow.

### Changed
- Unified public and organization package dependency resolution through a shared resolver.
- Split the public Git package catalog into individually editable data assets.

### Fixed
- Escaped branch names when requesting branch information from GitHub.

## [5.0.0] - 2026-07-25

### Breaking Changes
- Changed editor namespaces to the `ParkMinPackages.PackageManager.Editor` convention.

### Fixed
- Updated serialized package catalog type metadata to the new namespace.
## [2.1.2] - 2026-07-25

### Fixed
- Restored Unity's parameterless CreateGUI entry point for the Package Manager window.

## [2.1.1] - 2026-07-25

### Fixed
- Fixed Unity editor compilation errors in package catalog loading.

## [2.1.0] - 2026-07-25

### Changed
- Replaced per-repository GitHub API discovery with a central package catalog.
- Added a 15-minute local catalog cache and an expired-cache fallback when refresh fails.
- Compare installed and catalog package versions to determine update availability.

## [2.0.0] - 2026-07-25

### Breaking Changes
- Renamed public namespaces and assembly definitions from Mutant to ParkMinPackages.
- Projects using the previous namespaces or assembly names must update their references.

## [1.1.7] - 2026-07-24
### Changed
- Updated GitHub organization discovery to ParkMinPackages.
- Renamed the Package Manager menu and editor UI to ParkMinPackages.
- Made the GitHub Personal Access Token optional for public packages; it remains available for private packages.

## [1.1.2] - 2026-04-07
### gitingore 수정해서 .unitypackage 무시안하게 변경

## [1.1.1] - 2026-04-07
### 외부에서 참조 못하게 internal로 변경, assemblydefinition 옵션 변경

## [1.1.0] - 2026-04-07
### 필수 package 목록들 추가, 로컬 패키지 폴더 기능 추가

## [1.0.0] - 2026-04-07
### 실사용 가능 수준으로 완성

## [0.1.0] - 2026-04-06
### This is the first release of *\<Mutant.PakcageManager\>*.

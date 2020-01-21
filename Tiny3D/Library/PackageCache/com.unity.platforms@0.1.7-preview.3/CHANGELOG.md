# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.1.7-preview.3] - 2019-12-09

### Changed
* Disabled burst for windows/dotnet/collections checks, because it was broken.

## [0.1.7-preview.2] - 2019-11-12

### Changed
* Changed the way platforms customize builds for dots runtime, in a way that makes buildsettings usage clearer and faster, and more reliable.

## [0.1.7-preview] - 2019-10-25

### Added
* Added `WriteBeeConfigFile` method to pass build target specifc configuration to Bee.

## [0.1.6-preview] - 2019-10-23

### Added
* Re-introduce the concept of "buildable" build targets with the `CanBuild` property.

### Changed
* `GetDisplayName` method changed for `DisplayName` property.
* `GetUnityPlatformName` method changed for `UnityPlatformName` property.
* `GetExecutableExtension` method changed for `ExecutableExtension` property.
* `GetBeeTargetName` method changed for `BeeTargetName` property.

## [0.1.5-preview] - 2019-10-22

### Added
* Added static method `GetBuildTargetFromUnityPlatformName` to find build target that match Unity platform name. If build target is not found, an `UnknownBuildTarget` will be returned.
* Added static method `GetBuildTargetFromBeeTargetName` to find build target that match Bee target name. If build target is not found, an `UnknownBuildTarget` will be returned.

### Changed
* `AvailableBuildTargets` will now contain all build targets regardless of `HideInBuildTargetPopup` value, as well as `UnknownBuildTarget` instances.

## [0.1.4-preview] - 2019-09-26
* Bug fixes  
* Add iOS platform support
* Add desktop platforms package

## [0.1.3-preview] - 2019-09-03

* Bug fixes  

## [0.1.2-preview] - 2019-08-13

### Added
* Added static `AvailableBuildTargets` property to `BuildTarget` class, which provides the list of available build targets for the running Unity editor platform.
* Added static `DefaultBuildTarget` property to `BuildTarget` class, which provides the default build target for the running Unity editor platform.

### Changed
* Support for Unity 2019.1.

## [0.1.1-preview] - 2019-06-10

* Initial release of *Unity.Platforms*.

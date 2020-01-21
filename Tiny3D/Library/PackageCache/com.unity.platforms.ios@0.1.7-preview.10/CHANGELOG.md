# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.1.7-preview.10] - 2019-12-11

### Changed
* Revert `Using static libs in XCode project instead of dynamic`

## [0.1.7-preview.9] - 2019-12-10

### Changed
* Updated `com.unity.platforms` to `0.1.7-preview.3`.

## [0.1.7-preview.8] - 2019-12-09

### Changed
* Using static libs in XCode project instead of dynamic
* Forced OpenGLES for A7/A8 devices, should be reverted once problem with Metal are solved

## [0.1.7-preview.7] - 2019-12-05

### Changed
* Added missing launch screens for iPhone and iPad to template iOS project

## [0.1.7-preview.6] - 2019-12-04

### Changed
* Added missing file to template iOS project

## [0.1.7-preview.5] - 2019-11-21

### Changed
* Default icons added to template iOS project
* Basic multi-touch support is implemented
* Applications running full-screen on all devices support

## [0.1.7-preview.4] - 2019-11-21

### Changed
* Updated `Run` to open output folder, for now

## [0.1.7-preview.3] - 2019-11-12

### Changed
* Made `Identifier` and `Toolchain` public on `DotsIOSTarget`
* Get rid of iOS platform Run support (due to `ios-deploy` inconsistent behavior)
* `IOSAppToolchain` exports XCode project and opens it in Finder window

## [0.1.7-preview.2] - 2019-11-08

### Changed
* Improved error message when xcode installation is wrong

## [0.1.7-preview] - 2019-10-25

### Changed
* Updated `IOSAppToolchain` to use system iOS SDK
* Updated `com.unity.platforms` to `0.1.7-preview`.

## [0.1.6-preview] - 2019-10-23

### Added
* Added `CanBuild` property to all build targets.

### Changed
* Updated `com.unity.platforms` to `0.1.6-preview`.

## [0.1.5-preview] - 2019-10-22

### Changed
* Updated `com.unity.platforms` to `0.1.5-preview`.
* iOS build targets `HideInBuildTargetPopup` is now `false`.

## [0.1.4-preview] - 2019-09-26
* Bug fixes  
* Add iOS platform support
* Add desktop platforms package

## [0.1.3-preview] - 2019-09-03
Initial release

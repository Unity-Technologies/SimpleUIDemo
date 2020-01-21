# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.1.7-preview.6] - 2019-12-09

### Changed
* Rename the rendering library dependency 

## [0.1.7-preview.5] - 2019-12-04

### Changed
* Fix errors shown while building other platforms if the Android toolchain is not installed.

## [0.1.7-preview.4] - 2019-11-28

### Changed
* Got rid of GLES dependency
* Disabled thumb for Debug configuration to solve problem with Android Studio debugging
* Fixed problem with device volume keys not working

## [0.1.7-preview.3] - 2019-11-19

### Changed
* Show errors if `AndroidApkToolchain` is not found

## [0.1.7-preview.2] - 2019-11-12

### Changed
* Made `Identifier` and `Toolchain` public on `DotsAndroidTarget`
* Draft keyboard support is implemented
* Suspend/Resume support is implemented

## [0.1.7-preview] - 2019-10-25

### Added
* Added `WriteBeeConfigFile` method to pass Android SDK/NDK/JDK/Gradle paths to Bee

### Changed
* Updated `AndroidApkToolchain` to use Android SDK/NDK/JDK/Gradle from androidsettings.json file
* Updated `AndroidApkToolchain` to use NDK r19
* Updated `com.unity.platforms` to `0.1.7-preview`.

## [0.1.6-preview] - 2019-10-23

### Added
* Added `CanBuild` property to all build targets.

### Changed
* Updated `com.unity.platforms` to `0.1.6-preview`.

## [0.1.5-preview] - 2019-10-22

### Changed
* Updated `com.unity.platforms` to `0.1.5-preview`.

## [0.1.4-preview] - 2019-09-26
* Bug fixes  
* Add iOS platform support
* Add desktop platforms package

## [0.1.3-preview] - 2019-09-03

* Bug fixes  

## [0.1.2-preview] - 2019-08-13

### Changed

* Support for Unity 2019.1.

## [0.1.1-preview] - 2019-07-01

* Initial release of *Unity.Platforms.Android*.

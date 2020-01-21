@ECHO OFF
set bee=%~dp0..\PackageCache\com.unity.dots.runtime@0.1.0-preview.5\bee~\bee.exe
if [%1] == [] (%bee% -t) else (%bee% %*)

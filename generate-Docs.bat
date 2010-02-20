rem ==========================
rem Generates the documentation
rem ==========================
rem

tools\nant\nant.exe -buildfile:GitSharp.build %1 -t:net-3.5 -D:build.config=release -D:build.vcs.number.1=%BUILD_VCS_NUMBER% compile-gitsharp
cd tools\docu
docu ..\..\build\net-3.5-release\bin\GitSharp.dll

rem The docs are generated into tools\docu\output
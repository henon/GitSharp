rem ==========================
rem Generates the documentation
rem ==========================
rem
rem Note: this assumes, that you have built gitsharp using the build script before switching to gh-pages branch.

docu ../build/net-3.5-debug/bin/GitSharp.dll
xcopy /S /Y output\*.* ..
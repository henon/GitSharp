REM Note: this assues, that you have built gitsharp using the build script before switching to gh-pages branch.
docu ../build/net-3.5-release/bin/GitSharp.dll
xcopy /S /Y output\*.* ..
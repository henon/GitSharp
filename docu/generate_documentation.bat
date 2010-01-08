REM Note: this assues, that you have built gitsharp using visual studio before switching to gh-pages branch.
docu ../GitSharp/bin/Debug/GitSharp.dll
xcopy /S /Y output\*.* ..
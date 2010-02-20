rem ==========================
rem Generates the documentation
rem ==========================
rem
rem Note: this assumes, that you have built the debug version of gitsharp with visual studio.

docu ../GitSharp/bin/Debug/GitSharp.dll
rem xcopy /S /Y output\*.* ..
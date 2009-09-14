@echo off
if "%1" == "" goto BuildDefault
goto BuildTarget

:BuildDefault
%windir%\Microsoft.NET\Framework\v3.5\MSBuild.exe GitSharp.proj /p:Configuration=Debug /t:Clean /t:Build
goto End

:BuildTarget
%windir%\Microsoft.NET\Framework\v3.5\MSBuild.exe GitSharp.proj /p:Configuration=Debug /t:%*

:End

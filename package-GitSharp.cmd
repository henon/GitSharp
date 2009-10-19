extern\nant\nant.exe "-buildfile:GitSharp.build" "-D:build.config=release" "-D:build.platform=net-3.5" "-D:build.vcs.number=%BUILD_VCS_NUMBER%" clean dist
REM pause
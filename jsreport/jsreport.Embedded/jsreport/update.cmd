REM Updates jsreport version and packages the app into jsreport.zip

cmd.exe /C "cd app&&"../../.bin/npm.cmd" install jsreport --production --save-exact"
cmd.exe /C "install.cmd"
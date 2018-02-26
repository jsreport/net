REM This is being run by the nuget install and also if you run update.cmd, there should be no need to run it manually otherwise
set installScriptPath=%~1

if exist "install.js" (
    "../.bin/node.cmd" install.js   
    exit /b
)

if "%installScriptPath%" == "" (
    exit /b 87
)

if not exist "%installScriptPath%" (
    exit /b 2
)

"../.bin/node.cmd" "%installScriptPath%"
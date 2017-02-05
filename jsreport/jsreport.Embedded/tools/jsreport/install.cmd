REM This is being run by the nuget install and also if you run update.cmd, there should be no need to run it manually otherwise

if exist "install.js" (
    "../.bin/node.cmd" install.js    
) else (
    "../.bin/node.cmd" "../../packages/jsreport.Embedded.1.0.3/tools/install.js"
)



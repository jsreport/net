//This is being run by the nuget install, there should be no need to run it manually

var execSync = require('child_process').execSync;
var path = require('path');
var fs = require('fs');

console.log('Installing jsreport through NPM, this may take few minutes...');

var log = fs.openSync(path.join(__dirname, 'install-log.txt'), 'a');
var execOptions = {
    cwd: path.join(__dirname, 'development'),
    stdio: ['ignore', log, log]
};


try {
    execSync('"' + path.join(__dirname, '../.bin/npm.cmd') + '" install --production', execOptions);
    
    //remove some unnecesarry files
    execSync('rename node_modules\\phantomjs .phantomjs', execOptions);
    execSync('FOR /d /r . %d IN (phantomjs) DO @IF EXIST "%d" rd /s /q "%d"', execOptions);
    execSync('rename node_modules\\.phantomjs phantomjs', execOptions);
    execSync('del /S *.md');
    execSync('FOR /d /r . %d IN (test) DO @IF EXIST "%d" rd /s /q "%d"', execOptions);
    execSync('del /S *.png', execOptions);
    execSync('del /S LICENSE', execOptions);
    execSync('del /S .gitattributes', execOptions);
    execSync('del /S .npmignore', execOptions);
    execSync('del /S .gitignore', execOptions);
    execSync('del /S .travis', execOptions);
    execSync('del /S .babelrc', execOptions);
    execSync('del /S .eslintrc', execOptions);
    execSync('del /S .editorconfig', execOptions);
    execSync('del /S .jshintrc', execOptions);
    execSync('del /S *.map', execOptions);
    execSync('del /S Makefile', execOptions);
    execSync('xcopy node_modules\\phantom-html-to-pdf\\node_modules\\lodash node_modules\\lodash /Y', execOptions);
    execSync('rename node_modules\\lodash .lodash', execOptions);
    execSync('FOR /d /r . %d IN (lodash) DO @IF EXIST "%d" rd /s /q "%d"', execOptions);
    execSync('rename node_modules\\.lodash lodash', execOptions);
} catch (e) {
    console.error('npm install failed, see install-log.txt for details');
    process.exit(1);
}

console.log('npm install succeeded');


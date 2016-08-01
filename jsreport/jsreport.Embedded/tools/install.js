//This is being run by the nuget install, there should be no need to run it manually

var execSync = require('child_process').execSync;
var path = require('path');
var fs = require('fs');

var directoryExistsSync = function(file) {
    try {
        return fs.statSync(file).isDirectory();
    } catch(e) {
        return false;
    }
};


console.log('Installing jsreport through NPM, this may take few minutes...');

var cwd = process.cwd();

var log = fs.openSync(path.join(cwd, 'install-log.txt'), 'a');

process.on('uncaughtException', function (err) {
    fs.writeSync(log, err.stack);
    process.exit(1);
});

var packageJson = JSON.parse(fs.readFileSync(path.join(cwd, 'app', 'package.json')));

var installCommand = '"' + path.join(cwd, '../.bin/npm.cmd') + '" install jsreport --production --save --save-exact';
if (packageJson.dependencies.jsreport) {
    installCommand = '"' + path.join(cwd, '../.bin/npm.cmd') + '" install';
}

var execOptions = {
    cwd: path.join(cwd, 'app'),
    stdio: ['ignore', log, log]
};

try {
    execSync(installCommand, execOptions);
    
    if (directoryExistsSync(path.join(cwd, 'app', 'node_modules', 'phantom-html-to-pdf', 'node_modules', 'lodash'))) {
        execSync('xcopy node_modules\\phantom-html-to-pdf\\node_modules\\lodash node_modules\\lodash /Y', execOptions);
    }
    
    execSync('rename node_modules\\lodash .lodash', execOptions);
    execSync('FOR /d /r . %d IN (lodash) DO @IF EXIST "%d" rd /s /q "%d"', execOptions);
    execSync('rename node_modules\\.lodash lodash', execOptions);
    
    //remove some unnecesarry files
    execSync('rename node_modules\\phantomjs .phantomjs', execOptions);
    execSync('FOR /d /r . %d IN (phantomjs) DO @IF EXIST "%d" rd /s /q "%d"', execOptions);
    execSync('rename node_modules\\.phantomjs phantomjs', execOptions);
    
    execSync('del /S *.md.*');
    execSync('FOR /d /r . %d IN (test) DO @IF EXIST "%d" rd /s /q "%d"', execOptions);
    execSync('FOR /d /r . %d IN (source-map) DO @IF EXIST "%d" rd /s /q "%d"', execOptions);
    execSync('FOR /d /r . %d IN (browser-version) DO @IF EXIST "%d" rd /s /q "%d"', execOptions);
    execSync('FOR /d /r . %d IN (uglify-js) DO @IF EXIST "%d" rd /s /q "%d"', execOptions);
    execSync('FOR /d /r . %d IN (uglify-to-browserify) DO @IF EXIST "%d" rd /s /q "%d"', execOptions);
    execSync('del /S /q node_modules\\moment-timezone\\data\\unpacked', execOptions);
    execSync('del /S /q node_modules\\moment\\min', execOptions);
    execSync('del /S *.png.*', execOptions);
    execSync('del /S LICENSE.*', execOptions);
    execSync('del /S .gitattributes.*', execOptions);
    execSync('del /S .npmignore.*', execOptions);
    execSync('del /S .gitignore.*', execOptions);
    execSync('del /S .travis.*', execOptions);
    execSync('del /S .babelrc.*', execOptions);
    execSync('del /S .eslintrc.*', execOptions);
    execSync('del /S .editorconfig.*', execOptions);
    execSync('del /S .jshintrc.*', execOptions);
    execSync('del /S *.map.*', execOptions);
    execSync('del /S Makefile.*', execOptions);
    execSync('del /S *.ts.*', execOptions);
    
} catch (e) {
    fs.writeSync(log, e.stack);
    console.error('npm install failed, see install-log.txt for details');
    process.exit(1);
}

console.log('npm install succeeded');

console.log('Packing jsreport to be published');

try {
    fs.unlinkSync(path.join(cwd, 'jsreport.zip'));
} catch (e) {

}


var nodePath = process.execPath;
console.log('Copy node.exe from ' + nodePath + ' to app/node.exe');
try {
    var output = execSync('copy "' + nodePath + '" "' + path.join(cwd, 'app', 'node.exe') + '" /Y');
    console.log(output.toString());
}
catch (e) {
    fs.writeSync(log, e.stdout.toString());
    console.log(e.stdout.toString());
    process.exit(1);
}

var developmentPath = path.join(cwd, 'app');
var jsreportZip = path.join(cwd, 'jsreport.zip');

var command = 'Add-Type -A System.IO.Compression.FileSystem;' +
    '[IO.Compression.ZipFile]::CreateFromDirectory(\'' + developmentPath + '\', \'' + jsreportZip + '\');';

console.log('Zip app folder to jsreport.zip');
try {
    execSync('powershell -command ' + command);
}
catch (e) {
    console.log(e.stdout.toString());
    process.exit(1);
}

fs.unlinkSync(path.join(cwd, 'app', 'node.exe'));

fs.writeSync(log, 'Done');
console.log('Done');


var execSync = require('child_process').execSync;
var path = require('path');
var fs = require('fs');

console.log('Packing jsreport to be published');

try {
    fs.unlinkSync(path.join(__dirname, 'production', 'jsreport.zip'));
} catch (e) {

}


var nodePath = process.execPath;
console.log('Copy node.exe from ' + nodePath + ' to development/node.exe');
try {
    var output = execSync('copy "' + nodePath + '" "' + path.join(__dirname, 'development', 'node.exe') + '" /Y');
    console.log(output.toString());
}
catch (e) {
    console.log(e.stdout.toString());
    process.exit(1);
}

var developmentPath = path.join(__dirname, 'development');
var jsreportZip = path.join(__dirname, 'production', 'jsreport.zip');

var command = 'Add-Type -A System.IO.Compression.FileSystem;' +
    '[IO.Compression.ZipFile]::CreateFromDirectory(\'' + developmentPath + '\', \'' + jsreportZip + '\');';

console.log('Zip development folder to production/jsreport.zip');
try {
    execSync('powershell -command ' + command);
}
catch (e) {
    console.log(e.stdout.toString());
    process.exit(1);
}

fs.unlinkSync(path.join(__dirname, 'development', 'node.exe'));

console.log('Done');



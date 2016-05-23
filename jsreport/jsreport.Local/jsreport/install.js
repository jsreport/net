//This is being run by the nuget install, there should be no need to run it manually

var execSync = require('child_process').execSync;
var path = require('path');
var fs = require('fs');

console.log('Installing jsreport through NPM, this may take few minutes...');

var log = fs.openSync(path.join(__dirname, 'install-log.txt'), 'a');

try {
    var code = execSync('"' + path.join(__dirname, '../.bin/npm.cmd') + '" install --production', {
        cwd: path.join(__dirname, 'development'),
        stdio: ['ignore', log, log]
    });
} catch (e) {
    console.error('npm install failed, see install-log.txt for details');
    process.exit(1);
}

console.log('npm install succeeded');


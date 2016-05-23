var path = require('path');
var exec = require('child_process').exec;

var reporter = require('jsreport')({
    dataDirectory: path.join(__dirname, '../reports')
});

reporter.init().then(function() {
    exec('explorer ' + 'http://localhost:' + reporter.options.httpPort);
}).catch(function(e) {
    console.trace(e);
    throw e;
});


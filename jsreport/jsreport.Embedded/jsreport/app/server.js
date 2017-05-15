var path = require('path');
var exec = require('child_process').exec;

var reporter = require('jsreport')();

reporter.init().then(function () {
    if (process.env.NODE_ENV === 'development') {
        exec('explorer ' + 'http://localhost:' + reporter.options.httpPort);
    }
}).catch(function(e) {
    console.trace(e);
    throw e;
});


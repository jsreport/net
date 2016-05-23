
var fs = require('fs');
var uuid = require('uuid').v4;
var path = require('path');
var reporter = require('jsreport')({
    dataDirectory: path.join(__dirname, '../reports')
});

reporter.init().then(function () {
    var request = JSON.parse(process.env.JSREPORT_REQUEST);

    return reporter.render(request).then(function(resp) {
        var out = path.join(reporter.options.tempDirectory, uuid());
        var wstream = fs.createWriteStream(out);
        resp.stream.pipe(wstream);
        wstream.on('close', function() {
            console.log('$output=' + out);
            process.nextTick(function() {
                process.exit();
            });
        });
    });
}).catch(function (e) {
    console.error(e.stack);
    process.exit(1);
})

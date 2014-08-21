function formatDate(date) {
    return moment(date).format("MM-DD-YYYY");
}

function dangerClass(data, currentBudget) {
    var highest = _.max(data, function (d) { return d.Budget; });

    return highest.Budget == currentBudget ? "warning" : "";
}
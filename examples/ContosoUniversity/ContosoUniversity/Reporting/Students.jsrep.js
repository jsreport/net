function formatDate(date) {
    return moment(date).format("MM-DD-YYYY");
}

function getYear(date) {
    return moment(date).year();
}
function toJSON(data) {
    return JSON.stringify(data);
}

function mostSelling(books) {
    var max = { sales: 0 };
    books.forEach(function (b) {
        if (b.sales > max.sales) {
            max = b;
        }
    });

    return max.name + ' ' + max.sales;
}

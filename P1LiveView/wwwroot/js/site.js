"use strict";

window.myChart = null;

var connection = new signalR.HubConnectionBuilder().withUrl("/p1").build();

let prevMeasurement = null;

connection.on("ReceiveMeasurement", function (measurement) {
    if (prevMeasurement == null) {
        prevMeasurement = measurement;
        return;
    }

    if (window.myChart != null) {
        if (window.myChart.data.datasets[0].data.length >= 12) {
            window.myChart.data.datasets[0].data.shift();
        }
        if (window.myChart.data.datasets[1].data.length >= 12) {
            window.myChart.data.datasets[1].data.shift();
        }

        let deliverdTo = (measurement.electricityDeliveredToClientTariff1 + measurement.electricityDeliveredToClientTariff2);
        let prevDeliverdTo = (prevMeasurement.electricityDeliveredToClientTariff1 + prevMeasurement.electricityDeliveredToClientTariff2);

        window.myChart.data.datasets[0].data.push(deliverdTo - prevDeliverdTo);

        let deliveredBy = measurement.electricityDeliveredByClientTariff1 + measurement.electricityDeliveredByClientTariff2;
        let prevDeliveredBy = prevMeasurement.electricityDeliveredByClientTariff1 + prevMeasurement.electricityDeliveredByClientTariff2;

        window.myChart.data.datasets[1].data.push(deliveredBy - prevDeliveredBy);

        window.myChart.update();
    }

    prevMeasurement = measurement;
});

connection.start().then(function () {

}).catch(function (err) {
    return console.error(err.toString());
});

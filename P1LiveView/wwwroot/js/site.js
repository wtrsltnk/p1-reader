"use strict";

window.myChart = null;
window.maxMeasurementCount = 35;

var connection = new signalR.HubConnectionBuilder().withUrl("/p1").build();

let prevMeasurement = null;

function AddToChart(
    measurement,
    update) {
    if (prevMeasurement == null) {
        prevMeasurement = measurement;
        return;
    }

    var d = new Date(measurement.timeStamp);
    //if (d.getTime() - new Date(prevMeasurement.timeStamp).getTime() < 5000) {
    //    return;
    //}

    if (window.myChart != null) {
        if (window.myChart.data.datasets[0].data.length >= window.maxMeasurementCount) {
            window.myChart.data.datasets[0].data.shift();
            window.myChart.data.datasets[1].data.shift();
        }
        if (window.myChart.data.labels.length >= window.maxMeasurementCount) {
            window.myChart.data.labels.shift();
        }

        window.myChart.data.labels.push(`${d.getHours()}:${d.getMinutes() < 10 ? '0' : ''}${d.getMinutes()}:${d.getSeconds() < 10 ? '0' : ''}${d.getSeconds()}`);

        let deliverdTo = (measurement.electricityDeliveredToClientTariff1 + measurement.electricityDeliveredToClientTariff2);
        let prevDeliverdTo = (prevMeasurement.electricityDeliveredToClientTariff1 + prevMeasurement.electricityDeliveredToClientTariff2);

        window.myChart.data.datasets[0].data.push(deliverdTo - prevDeliverdTo);

        let deliveredBy = measurement.electricityDeliveredByClientTariff1 + measurement.electricityDeliveredByClientTariff2;
        let prevDeliveredBy = prevMeasurement.electricityDeliveredByClientTariff1 + prevMeasurement.electricityDeliveredByClientTariff2;

        window.myChart.data.datasets[1].data.push(deliveredBy - prevDeliveredBy);

        if (update) {
            window.myChart.update();
        }
    }

    prevMeasurement = measurement;

}
connection.on("ReceiveMeasurement", function (measurement) {
    AddToChart(measurement, true);
});

connection.start().then(function () {

}).catch(function (err) {
    return console.error(err.toString());
});

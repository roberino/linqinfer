'use strict';

angular.module('linqinfer.sofm', ['ngRoute'])

.config(['$routeProvider', function($routeProvider) {
    $routeProvider.when('/sofm/:resourceUrl*', {
        templateUrl: '/app/sofm/sofm.html',
        controller: 'SofmCtrl'
  });
}])

.controller('SofmCtrl', ['$scope', '$routeParams', '$resource', function ($scope, $routeParams, $resource) {
    //alert($routeParams.resourceUrl);
    var Sofm = $resource('/api/' + $routeParams.resourceUrl + '/sofm');
    var sofm = Sofm.get({ }, function () {
        $scope.data = sofm.map;
        $scope.data.labels = getValues(sofm.features);
        $scope.chart = {
            xAxis: $scope.data.labels[0],
            yAxis: $scope.data.labels[1]
        };
        google.charts.setOnLoadCallback(drawChart);

        $scope.$watch('chart.xAxis', function (newValue, oldValue) {
            if (newValue !== oldValue) google.charts.setOnLoadCallback(drawChart);
        });
        $scope.$watch('chart.yAxis', function (newValue, oldValue) {
            if (newValue !== oldValue) google.charts.setOnLoadCallback(drawChart);
        });
    });

    var i = 0;

    function drawChart() {

        var xi = $scope.chart.xAxis.value;
        var yi = $scope.chart.yAxis.value;

        var options = {
            title: 'SOFM',
            hAxis: { title: $scope.chart.xAxis.key },
            vAxis: { title: $scope.chart.yAxis.key },
            bubble: { textStyle: { fontSize: 11 } }
        };

        var rows = [];
        var len = $scope.data.length;
        rows.push(['Euclidean Length', $scope.chart.xAxis.key, $scope.chart.yAxis.key, 'Node', 'Number Of Members']);

        $scope.data.forEach(function (x) {
            i++;
            rows.push([Math.round(x.euclideanLength * 100) / 100 + '', x.weights[yi], x.weights[xi], i, x.numberOfMembers]);
        });

        var data = google.visualization.arrayToDataTable(rows);

        // Instantiate and draw the chart.
        var chart = new google.visualization.BubbleChart(document.getElementById('sofmChart'));

        chart.draw(data, options);

        google.visualization.events.addListener(chart, 'select', selectHandler);

        function selectHandler(e) {
            alert(data.getValue(chart.getSelection()[0].row, 0));
        }
    }


    function getValues(hash) {
        var values = [];
        for (var key in hash) {
            if (hash.hasOwnProperty(key)) {
                values.push({
                    key: key,
                    value: hash[key]
                });
            }
        }
        return values;
    };

}]);
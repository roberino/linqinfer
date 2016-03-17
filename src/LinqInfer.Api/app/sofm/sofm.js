'use strict';

angular.module('linqinfer.sofm', ['ngRoute'])

.config(['$routeProvider', function($routeProvider) {
    $routeProvider.when('/sofm/:resourceUrl*', {
        templateUrl: '/app/sofm/sofm.html',
        controller: 'SofmCtrl'
  });
}])

.controller('SofmCtrl', ['$scope', '$resource', function ($scope, $resource) {
    var Sofm = $resource('/api/data/samples/:id/sofm');
    var sofm = Sofm.get({ id: '37a681e6-e4fe-4df3-95ab-3252d38109ec' }, function () {
        $scope.data = sofm.map;
        $scope.data.labels = getValues(sofm.features);
        $scope.chart = {
            xAxis: { value: 0 },
            yAxis: { value: 0 }
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
            hAxis: { title: 'Number' },
            vAxis: { title: 'Euclidean Length' },
            bubble: { textStyle: { fontSize: 11 } }
        };

        var rows = [];
        var len = $scope.data.length;
        rows.push(['ID', 'Number', 'Euclidean Length', 'a', 'Number Of Members']);

        $scope.data.forEach(function (x) {
            rows.push([i + 'id', x.weights[yi], x.weights[xi], 'a', x.numberOfMembers]);
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
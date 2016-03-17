'use strict';

angular.module('linqinfer.samples', ['ngRoute'])

.config(['$routeProvider', function($routeProvider) {
  $routeProvider.when('/samples', {
    templateUrl: '/app/samples/samples.html',
    controller: 'SampleCtrl'
  });
}])

.controller('SampleCtrl', ['$scope', '$resource', '$location', function ($scope, $resource, $location) {


    var lookups = {};

    var Samples = $resource('/api/data/samples');
    var samples = Samples.get({ }, function () {

        $scope.data = samples.items;

        google.charts.setOnLoadCallback(drawChart);
    });

    function drawChart() {
        // Define the chart to be drawn.
        var data = new google.visualization.DataTable();
        data.addColumn('string', 'Sample');
        data.addColumn('number', 'Percentage');
        var rows = [];
        var len = $scope.data.length;

        $scope.data.forEach(function (x) {
            rows.push([x.name, 1 / len]);
            lookups[x.name] = x.path;
        });

        data.addRows(rows);

        // Instantiate and draw the chart.
        var chart = new google.visualization.BarChart(document.getElementById('sampleChart'));

        chart.draw(data, null);

        google.visualization.events.addListener(chart, 'select', selectHandler);

        function selectHandler(e) {
            var selection = chart.getSelection()[0];
            if (selection) {
                var sampleName = data.getValue(selection.row, 0);
                var path = '/sofm' + lookups[sampleName];
                $scope.$apply(function () { $location.path(path); });
            }
        }
    }
}]);
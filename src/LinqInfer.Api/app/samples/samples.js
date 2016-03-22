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

    $scope.getSofmPath = function (apiPath) {
        return '/sofm/' + getRelPath(apiPath);
    };

    function getRelPath(apiPath) {
        var n = apiPath.indexOf('/api');
        return apiPath.substring(n + 5);
    }

    function drawChart() {
        // Define the chart to be drawn.
        var data = new google.visualization.DataTable();
        data.addColumn('string', 'Sample');
        data.addColumn('number', 'Count');
        var rows = [];
        var len = $scope.data.length;

        $scope.data.forEach(function (x) {
            rows.push([x.header.label, x.header.summary.count]);
            lookups[x.header.label] = getRelPath(x.uri);
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
                var path = '/sofm/' + lookups[sampleName];
                $scope.$apply(function () { $location.path(path); });
            }
        }
    }
}]);
'use strict';

// Declare app level module which depends on views, and components
angular.module('linqinfer', [
  'ngRoute',
  'ngResource',
  'linqinfer.samples',
  'linqinfer.sofm',
  'linqinfer.version'
]).
config(['$routeProvider', function($routeProvider) {
  $routeProvider.otherwise({redirectTo: '/samples'});
}]);

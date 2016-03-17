'use strict';

angular.module('linqinfer.version', [
  'linqinfer.version.interpolate-filter',
  'linqinfer.version.version-directive'
])

.value('version', '0.1');

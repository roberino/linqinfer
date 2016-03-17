'use strict';

describe('linqinfer.version module', function () {
    beforeEach(module('linqinfer.version'));

  describe('version service', function() {
    it('should return current version', inject(function(version) {
      expect(version).toEqual('0.1');
    }));
  });
});

define(['baseView'], function (BaseView) {
  'use strict';
  function View(view, params) { BaseView.apply(this, arguments); }
  Object.assign(View.prototype, BaseView.prototype);
  View.prototype.onResume = function () {
    BaseView.prototype.onResume.apply(this, arguments);
    window.location.replace(ApiClient.getUrl('LibraryHub/Browse'));
  };
  return View;
});

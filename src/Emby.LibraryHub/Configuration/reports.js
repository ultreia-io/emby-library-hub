define(['baseView'], function (BaseView) {
  'use strict';
  function View(view, params) { BaseView.apply(this, arguments); }
  Object.assign(View.prototype, BaseView.prototype);
  View.prototype.onResume = function () {
    BaseView.prototype.onResume.apply(this, arguments);
    var params = this.params || {};
    var subscription = false || ['confirm', 'unsubscribe'].indexOf(params.action) !== -1;
    var path = 'LibraryHub/' + (subscription ? 'Subscriptions' : 'Archive');
    if (!subscription && /^[0-9]{4}-[0-9]{2}-[0-9]{2}$/.test(params.day || '')) path += '/' + params.day;
    var url = ApiClient.getUrl(path);
    if (subscription && ['confirm', 'unsubscribe'].indexOf(params.action) !== -1) {
      url += '?action=' + params.action;
      if (/^[a-f0-9]{64}$/i.test(params.token || '')) url += '&token=' + params.token;
      if (params.lang === 'fr' || params.lang === 'en') url += '&lang=' + params.lang;
    }
    window.location.replace(url);
  };
  return View;
});

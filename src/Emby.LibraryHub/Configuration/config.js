define(['baseView', 'loading', ApiClient.getUrl('web/ConfigurationPage', {name: 'libraryhublocalejs'}), 'emby-input', 'emby-button', 'emby-select', 'emby-checkbox', 'emby-scroller', 'emby-linkbutton'], function (BaseView, loading, locale) {
  'use strict';

  function View(view, params) {
    BaseView.apply(this, arguments);
    var pluginId = 'f6975142-a690-4cdc-b98b-2c43b2d084ed';
    var page = view;
    var i18n = locale.create(page);
    var t = i18n.text;
    var fields = ['Language', 'TimeZoneId', 'PublicServerUrl', 'Sender', 'SmtpHost', 'SmtpUsername', 'SmtpPassword'];
    var switches = ['DeliveryEnabled', 'UseStartTls', 'IncludeVideoLinks', 'SubscriptionsEnabled'];
    var numbers = ['SmtpPort', 'DeliveryHour', 'DeliveryMinute'];
    var browseLibraries = [], browseLibraryIds = null, browseLibrariesLoaded = false;
    var busy = false;
    var reportsBusy = false;
    var reportToday = '';

    function displayDay(value) { return (value || '').replace(/-/g, '/'); }
    function inputDay(id) { return field(id).value.trim().replace(/\//g, '-'); }
    ['HistoryFrom', 'HistoryTo'].forEach(function (id) {
      var picker = field(id + 'Picker');
      var button = field(id + 'Calendar');
      button.hidden = typeof picker.showPicker !== 'function';
      button.addEventListener('click', function () {
        picker.value = validDay(inputDay(id)) ? inputDay(id) : reportToday;
        picker.max = reportToday;
        picker.showPicker();
      });
      picker.addEventListener('change', function () {
        if (picker.value) field(id).value = displayDay(picker.value);
      });
      field(id).addEventListener('blur', function () {
        if (validDay(inputDay(id))) field(id).value = displayDay(inputDay(id));
      });
    });

    function field(id) { return page.querySelector('#' + id); }
    function setBusy(value) {
      busy = value;
      ['saveButton', 'saveSubscriptionsButton', 'exportButton', 'restoreButton'].forEach(function (id) { field(id).disabled = value; });
      updateReportButtons();
      if (value) loading.show(); else loading.hide();
    }
    function reportError(error) {
      field('digestStatus').textContent = t('OperationFailed');
      console.error('Library Hub request failed', error && error.status);
    }
    function render(options) {
      fields.forEach(function (name) { field(name).value = options[name] || ''; });
      switches.forEach(function (name) { field(name).checked = options[name]; });
      browseLibraryIds = options.BrowseLibraryIds == null ? null : options.BrowseLibraryIds.slice();
      renderBrowseLibraries();
      field('SmtpPort').value = options.SmtpPort;
      field('DeliveryTime').value = ('0' + options.DeliveryHour).slice(-2) + ':' + ('0' + options.DeliveryMinute).slice(-2);
    }
    function renderBrowseLibraries() {
      var container = field('browseLibraryChoices'); container.textContent = '';
      browseLibraries.forEach(function (library) {
        var wrapper = document.createElement('div'); wrapper.className = 'checkboxContainer';
        var label = document.createElement('label'), input = document.createElement('input');
        input.type = 'checkbox'; input.value = library.Id;
        input.checked = browseLibraryIds === null || browseLibraryIds.indexOf(library.Id) !== -1;
        input.addEventListener('change', function () {
          browseLibraryIds = Array.from(container.querySelectorAll('input:checked')).map(function (n) { return n.value; });
        });
        label.appendChild(input); label.appendChild(document.createTextNode(' ' + library.Name));
        wrapper.appendChild(label); container.appendChild(wrapper);
      });
    }
    function loadBrowseLibraries() {
      browseLibrariesLoaded = false;
      return ApiClient.getJSON(ApiClient.getUrl('LibraryHub/Browse/Libraries')).then(function (result) {
        browseLibraries = result.Libraries; browseLibrariesLoaded = true; renderBrowseLibraries();
        field('browseLibrariesStatus').textContent = browseLibraries.length ? '' : t('NoBrowseLibraries');
      }).catch(function () { field('browseLibrariesStatus').textContent = t('BrowseLibrariesFailed'); });
    }
    function backupError(message) {
      var error = new Error(message);
      error.backupMessage = message;
      return error;
    }
    function readBackup(text) {
      var backup;
      try { backup = JSON.parse(text); }
      catch (_) { throw backupError(t('InvalidJson')); }
      if (!backup || backup.PluginId !== pluginId || backup.FormatVersion !== 1 || !backup.Digest || Array.isArray(backup.Digest))
        throw backupError(t('UnsupportedBackup'));
      var source = Object.assign({}, backup.Digest);
      // Older exports predate public subscriptions. Restoring them leaves the feature off.
      if (!Object.prototype.hasOwnProperty.call(source, 'SubscriptionsEnabled')) source.SubscriptionsEnabled = false;
      var options = {};
      fields.concat(switches, numbers).forEach(function (name) {
        var expected = fields.indexOf(name) !== -1 ? 'string' : switches.indexOf(name) !== -1 ? 'boolean' : 'number';
        if (!Object.prototype.hasOwnProperty.call(source, name) || typeof source[name] !== expected ||
            (expected === 'number' && !Number.isInteger(source[name])))
          throw backupError(t('InvalidSetting', name));
        options[name] = source[name];
      });
      // Semantic validation (time zone, SMTP, URLs) also runs on the server before it saves.
      if (['fr', 'en'].indexOf(options.Language) === -1 || options.DeliveryHour < 0 || options.DeliveryHour > 23 ||
          options.DeliveryMinute < 0 || options.DeliveryMinute > 59 || options.SmtpPort < 1 || options.SmtpPort > 65535)
        throw backupError(t('InvalidBackupValues'));
      if (source.BrowseLibraryIds != null && (!Array.isArray(source.BrowseLibraryIds) ||
          source.BrowseLibraryIds.some(function (id) { return typeof id !== 'string' || !/^[1-9][0-9]*$/.test(id); })))
        throw backupError(t('InvalidSetting', 'BrowseLibraryIds'));
      options.BrowseLibraryIds = source.BrowseLibraryIds == null ? null : source.BrowseLibraryIds.slice();
      return options;
    }

    function updateReportButtons() {
      ['generateReportsButton', 'resetArchiveButton', 'HistoryFrom', 'HistoryTo',
        'HistoryFromCalendar', 'HistoryFromPicker', 'HistoryToCalendar', 'HistoryToPicker'].forEach(function (id) {
          field(id).disabled = busy || reportsBusy;
        });
    }
    function loadCalendar() {
      return ApiClient.getJSON(ApiClient.getUrl('LibraryHub/Reports/Calendar')).then(function (result) {
        reportToday = result.Today;
        if (!field('HistoryFrom').value) field('HistoryFrom').value = displayDay(result.Today);
        if (!field('HistoryTo').value) field('HistoryTo').value = displayDay(result.Today);
      }).catch(function () {
        field('reportsStatus').textContent = t('CalendarFailed');
      });
    }
    function showPublicArchive(result) {
      if (result.Path !== 'emby/LibraryHub/Archive') return;
      var base = field('PublicServerUrl').value.trim().replace(/\/+$/, '');
      var url = base ? base + '/' + result.Path : ApiClient.getUrl('LibraryHub/Archive');
      field('PublicArchiveUrl').value = url;
      field('publicArchiveLink').href = url;
    }
    function loadPublicArchive() {
      return ApiClient.getJSON(ApiClient.getUrl('LibraryHub/Reports/Link')).then(showPublicArchive).catch(function () {
        field('PublicArchiveUrl').value = t('ArchiveUnavailable');
      });
    }
    field('resetArchiveButton').addEventListener('click', function () {
      if (busy || reportsBusy) return;
      reportsBusy = true; updateReportButtons();
      post('LibraryHub/Reports/Reset', {}).then(function (result) {
        showPublicArchive(result);
        field('archiveStatus').textContent = t('ArchiveReset', result.Count);
      }).catch(function () {
        field('archiveStatus').textContent = t('ArchiveResetFailed');
      }).then(function () { reportsBusy = false; updateReportButtons(); });
    });
    field('generateReportsButton').addEventListener('click', function () {
      if (busy || reportsBusy) return;
      var from = inputDay('HistoryFrom'), to = inputDay('HistoryTo');
      if (!validDay(from) || !validDay(to) || from > to || (reportToday && to > reportToday)) {
        field('reportsStatus').textContent = t('InvalidDates');
        return;
      }
      reportsBusy = true; updateReportButtons();
      field('reportsStatus').textContent = t('GeneratingReports');
      post('LibraryHub/Reports/Generate', {From: from, To: to}).then(function (result) {
        if (result.Error) { field('reportsStatus').textContent = i18n.error(result.Error); return; }
        showPublicArchive(result);
        field('reportsStatus').textContent = t('ReportsGenerated', result.Count);
      }).catch(function () {
        field('reportsStatus').textContent = t('GenerationFailed');
      }).then(function () { reportsBusy = false; updateReportButtons(); });
    });

    function renderSubscribers(result) {
      var body = field('subscriberRows');
      body.textContent = '';
      var confirmed = 0;
      result.Subscribers.forEach(function (subscriber) {
        if (subscriber.Status === 'Subscribed') confirmed++;
        var row = document.createElement('tr');
        [subscriber.Email, subscriber.Language === 'fr' ? 'Français' : 'English',
          t(subscriber.Status) + (subscriber.Error ? ' — ' + t(subscriber.Error) : '')].forEach(function (value) {
          var cell = document.createElement('td'); cell.textContent = value; cell.style.padding = '.6em'; row.appendChild(cell);
        });
        var actions = document.createElement('td');
        var remove = document.createElement('button');
        remove.type = 'button'; remove.className = 'raised'; remove.textContent = t('Unsubscribe');
        remove.addEventListener('click', function () {
          remove.disabled = true;
          post('LibraryHub/Subscribers/Remove', {Id: subscriber.Id}).then(renderSubscribers).catch(function () {
            field('subscribersStatus').textContent = t('RemoveFailed');
            remove.disabled = false;
          });
        });
        actions.appendChild(remove); row.appendChild(actions); body.appendChild(row);
      });
      field('subscribersStatus').textContent = t('SubscriberCount', confirmed, result.Subscribers.length - confirmed);
    }
    function loadSubscribers() {
      field('refreshSubscribersButton').disabled = true;
      return ApiClient.getJSON(ApiClient.getUrl('LibraryHub/Subscribers')).then(renderSubscribers).catch(function () {
        field('subscribersStatus').textContent = t('SubscribersFailed');
      }).then(function () { field('refreshSubscribersButton').disabled = false; });
    }
    field('refreshSubscribersButton').addEventListener('click', loadSubscribers);

    function post(route, data) {
      return ApiClient.ajax({type: 'POST', url: ApiClient.getUrl(route), dataType: 'json',
        contentType: 'application/json', data: JSON.stringify(data)});
    }
    function validDay(value) {
      if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
      var date = new Date(value + 'T00:00:00Z');
      return !isNaN(date.getTime()) && date.toISOString().slice(0, 10) === value;
    }
    field('browsePageLink').href = ApiClient.getUrl('LibraryHub/Browse');
    field('searchPageLink').href = ApiClient.getUrl('LibraryHub/Search');
    field('digestLogo').src = ApiClient.getUrl('LibraryHub/Assets/logo.svg');
    this.load = function () {
      if (busy) return;
      setBusy(true);
      return i18n.load().then(function () { return ApiClient.getPluginConfiguration(pluginId); }).then(function (config) {
        render(config.Digest);
        return Promise.all([loadCalendar(), loadPublicArchive(), loadSubscribers(), loadBrowseLibraries()]);
      }).catch(reportError).then(function () { setBusy(false); });
    };

    field('digestForm').addEventListener('submit', function (event) {
      event.preventDefault();
      if (busy) return;
      setBusy(true);
      ApiClient.getPluginConfiguration(pluginId).then(function (config) {
        var options = config.Digest;
        delete options.Recipient;
        if (browseLibrariesLoaded) options.BrowseLibraryIds = Array.from(field('browseLibraryChoices').querySelectorAll('input:checked')).map(function (n) { return n.value; });
        fields.forEach(function (name) { options[name] = field(name).value; });
        switches.forEach(function (name) { options[name] = field(name).checked; });
        options.SmtpPort = Number(field('SmtpPort').value);
        var time = field('DeliveryTime').value.split(':');
        options.DeliveryHour = Number(time[0]);
        options.DeliveryMinute = Number(time[1]);
        return ApiClient.updatePluginConfiguration(pluginId, config);
      }).then(function () {
        field('digestStatus').textContent = t('SettingsSaved');
      }).catch(reportError).then(function () { setBusy(false); });
    });

    field('exportButton').addEventListener('click', function () {
      if (busy) return;
      setBusy(true);
      ApiClient.getPluginConfiguration(pluginId).then(function (config) {
        var backup = {PluginId: pluginId, FormatVersion: 1, Digest: config.Digest};
        var text = JSON.stringify(backup, null, 2) + '\n';
        backup.Digest = readBackup(text);
        text = JSON.stringify(backup, null, 2) + '\n';
        var url = URL.createObjectURL(new Blob([text], {type: 'application/json;charset=utf-8'}));
        var link = document.createElement('a');
        link.href = url;
        link.download = 'emby-library-hub-configuration.json';
        page.appendChild(link);
        link.click();
        link.remove();
        setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
        field('digestStatus').textContent = t('Exported');
      }).catch(reportError).then(function () { setBusy(false); });
    });

    field('restoreButton').addEventListener('click', function () {
      if (busy) return;
      field('restoreFile').value = '';
      field('restoreFile').click();
    });
    field('restoreFile').addEventListener('change', function () {
      var file = field('restoreFile').files[0];
      if (!file || busy) return;
      if (file.size > 65536) {
        field('digestStatus').textContent = t('BackupTooLarge');
        return;
      }
      setBusy(true);
      file.text().then(function (text) {
        var options = readBackup(text);
        return ApiClient.getPluginConfiguration(pluginId).then(function (config) {
          config.Digest = options;
          return ApiClient.updatePluginConfiguration(pluginId, config).then(function () {
            render(options);
            field('digestStatus').textContent = t('Restored');
          });
        });
      }).catch(function (error) {
        if (error.backupMessage) field('digestStatus').textContent = error.backupMessage;
        else reportError(error);
      }).then(function () { setBusy(false); });
    });

    field('previewButton').addEventListener('click', function () {
      field('previewButton').disabled = true;
      field('digestStatus').textContent = t('PreparingPreview');
      ApiClient.getJSON(ApiClient.getUrl('LibraryHub/Preview')).then(function (message) {
        field('digestPreview').textContent = message.Body;
        field('digestStatus').textContent = t('Subject', message.Subject);
      }).catch(reportError).then(function () { field('previewButton').disabled = false; });
    });
  }

  Object.assign(View.prototype, BaseView.prototype);
  View.prototype.onResume = function (options) {
    BaseView.prototype.onResume.apply(this, arguments);
    return this.load();
  };
  return View;
});

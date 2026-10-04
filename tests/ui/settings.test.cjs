const {test} = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const {loadLocale, localizedPage} = require('./locale-fixture.cjs');
const controllerScript = fs.readFileSync(path.join(__dirname, '../../src/Emby.LibraryHub/Configuration/config.js'), 'utf8');
const html = fs.readFileSync(path.join(__dirname, '../../src/Emby.LibraryHub/Configuration/config.html'), 'utf8');

function fixture(uiLocale = 'en-US') {
    const elements = new Map([...html.matchAll(/\bid="([^"]+)"/g)].map(([, id]) => [id, {
        value: '', checked: false, disabled: false, textContent: '', handlers: {}, files: [], clicks: 0, children: [],
        appendChild(child) { this.children.push(child); },
        click() { this.clicks++; },
        showPicker() { this.pickerOpened = true; },
        addEventListener(event, action) { this.handlers[event] = action; }
    }]));
    const page = elements.get('libraryDigestPage');
    page.querySelector = selector => elements.get(selector.slice(1));
    const localized = localizedPage(page, html, elements);
    let localeModule;
    let configuration = {Digest: {
        BrowseLibraryIds: null, DeliveryEnabled: false, SubscriptionsEnabled: false, IncludeVideoLinks: true, Language: 'fr', TimeZoneId: 'Europe/Paris',
        PublicServerUrl: 'https://media.example.test', DeliveryHour: 8, DeliveryMinute: 5,
        SmtpHost: 'smtp.example.test', SmtpPort: 587, UseStartTls: true,
        Sender: 'sender@example.test', SmtpUsername: 'test', SmtpPassword: 'secret-test'
    }};
    const calls = [];
    const downloads = [];
    const requests = [];
    const timers = new Map();
    let timerId = 0;
    let subscribers = [];
    page.appendChild = function () {};
    const context = {
        console, Blob,
        Emby: {async importModule(module) {
            assert.equal(module, './modules/common/globalize.js');
            return {getCurrentLocale: () => uiLocale};
        }},
        URL: {
            createObjectURL(blob) { downloads.push({blob}); return 'blob:test-download'; },
            revokeObjectURL(url) { assert.equal(url, 'blob:test-download'); }
        },
        setTimeout(action, delay) {
            if (delay === 1000) { action(); return 0; }
            timers.set(++timerId, action); return timerId;
        },
        clearTimeout(id) { timers.delete(id); },
        document: {
            createElement(tag) {
                if (tag !== 'a') return {textContent: '', style: {}, children: [], handlers: {}, appendChild(child) { this.children.push(child); }, addEventListener(event, action) { this.handlers[event] = action; }};
                return {click() { downloads[downloads.length - 1].name = this.download; }, remove() {}};
            }
        },
        define(dependencies, factory) {
            assert.equal(dependencies[0], 'baseView');
            assert.equal(dependencies[2], 'https://media.example.test/emby/web/ConfigurationPage?name=libraryhublocalejs');
            function BaseView(view) { this.view = view; }
            BaseView.prototype.onResume = function () {};
            const View = factory(BaseView, {show() {}, hide() {}}, localeModule);
            context.controller = new View(page, {});
        },
        ApiClient: {
            async getPluginConfiguration() { return structuredClone(configuration); },
            async updatePluginConfiguration(id, config) { calls.push('save'); configuration = structuredClone(config); },
            getUrl: (route, query) => query ? 'https://media.example.test/emby/' + route + '?' + new URLSearchParams(query) : route,
            async ajax(request) {
                assert.equal(request.type, 'POST');
                assert.equal(request.contentType, 'application/json');
                assert.equal(request.dataType, 'json');
                const data = JSON.parse(request.data);
                requests.push({route: request.url, data});
                if (request.url === 'LibraryHub/Subscribers/Remove') { subscribers = subscribers.filter(s => s.Id !== data.Id); return {Path: 'web/index.html#!/configurationpage?name=libraryhubsubscriptions', Subscribers: subscribers}; }
                if (request.url === 'LibraryHub/Reports/Reset') return {Count: 268, Path: 'emby/LibraryHub/Archive'};
                if (request.url === 'LibraryHub/Reports/Generate') return {Count: 2, Path: 'emby/LibraryHub/Archive'};
                throw new Error('Unexpected endpoint ' + request.url);
            },
            async getJSON(route) {
                if (route === 'LibraryHub/Subscribers') return {Path: 'web/index.html#!/configurationpage?name=libraryhubsubscriptions', Subscribers: subscribers};
                if (route === 'LibraryHub/Reports/Link') return {Path: 'emby/LibraryHub/Archive'};
                if (route === 'LibraryHub/Reports/Calendar') return {Today: '2026-10-03'};
                assert.equal(route, 'LibraryHub/Preview'); calls.push('preview');
                return {Subject: 'Médiathèque', Body: '<script>untrusted title</script>\nAjouts :\n• Éléphant'};
            }
        }
    };
    localeModule = loadLocale(context);
    vm.runInNewContext(controllerScript, context);
    const flush = () => new Promise(resolve => setImmediate(resolve));
    return {elements, page, calls, downloads, flush, requests, timers, localized,
        setUiLocale(value) { uiLocale = value; },
        setSubscribers(value) { subscribers = value; },
        async poll() { const entries = [...timers.entries()]; timers.clear(); entries.forEach(([, action]) => action()); await flush(); },
        restore(text) {
            elements.get('restoreFile').files = [{size: text.length, text: async () => text}];
            elements.get('restoreFile').handlers.change();
            return flush();
        }, show: () => context.controller.onResume({}), config: () => configuration, context};
}

test('settings load saved values including switches and exact delivery time', async () => {
    const f = fixture(); f.show(); await f.flush();
    assert.equal(f.elements.get('Language').value, 'fr');
    assert.equal(f.elements.get('DeliveryTime').value, '08:05');
    assert.equal(f.elements.get('IncludeVideoLinks').checked, true);
    assert.equal(f.elements.get('DeliveryEnabled').checked, false);
});

test('save applies edited language, timing and link settings through Emby configuration API', async () => {
    const f = fixture(); f.show(); await f.flush();
    f.elements.get('Language').value = 'en'; f.elements.get('DeliveryTime').value = '19:35';
    f.elements.get('IncludeVideoLinks').checked = false;
    f.elements.get('digestForm').handlers.submit({preventDefault() {}}); await f.flush();
    assert.deepEqual(f.calls, ['save']);
    assert.equal(f.config().Digest.Language, 'en'); assert.equal(f.config().Digest.DeliveryHour, 19);
    assert.equal(f.config().Digest.DeliveryMinute, 35); assert.equal(f.config().Digest.IncludeVideoLinks, false);
});

test('preview is read-only and renders media titles as text', async () => {
    const f = fixture(); f.elements.get('previewButton').handlers.click(); await f.flush();
    assert.deepEqual(f.calls, ['preview']);
    assert.equal(f.elements.get('previewButton').disabled, false);
    assert.match(f.elements.get('digestPreview').textContent, /<script>untrusted title<\/script>/);
    assert.equal(f.elements.get('digestPreview').innerHTML, undefined);
});

test('failed preview displays an error and re-enables its button', async () => {
    const f = fixture(); f.context.ApiClient.getJSON = async () => { throw {status: 503}; };
    f.elements.get('previewButton').handlers.click(); await f.flush();
    assert.match(f.elements.get('digestStatus').textContent, /Operation failed/);
    assert.equal(f.elements.get('previewButton').disabled, false);
});

// Emby 4.11 loads data-controller through AMD; scripts inserted as HTML are inert.
test('page uses the registered AMD controller instead of inline scripts', () => {
    assert.match(html, /data-controller="__plugin\/libraryhubjs"/);
    assert.doesNotMatch(html, /<script/i);
    assert.match(html, /class="view /);
    const f = fixture();
    assert.equal(typeof f.context.controller.onResume, 'function');
});

function backup(options) {
    return JSON.stringify({PluginId: 'f6975142-a690-4cdc-b98b-2c43b2d084ed', FormatVersion: 1, Digest: options});
}

test('export downloads complete saved settings, including credentials, without saving unsaved edits', async () => {
    const f = fixture(); f.show(); await f.flush();
    f.elements.get('Language').value = 'en';
    f.elements.get('exportButton').handlers.click(); await f.flush();
    assert.deepEqual(f.calls, []);
    const download = f.downloads[0];
    assert.equal(download.name, 'emby-library-hub-configuration.json');
    const exported = JSON.parse(await download.blob.text());
    assert.equal(exported.FormatVersion, 1);
    assert.deepEqual(exported.Digest, f.config().Digest);
    assert.equal(exported.Digest.Language, 'fr');
    assert.equal(exported.Digest.SmtpPassword, 'secret-test');
});

test('exported JSON restores settings in one operation and updates the form', async () => {
    const source = fixture(); source.elements.get('exportButton').handlers.click(); await source.flush();
    const text = await source.downloads[0].blob.text();
    const target = fixture(); target.show(); await target.flush();
    target.elements.get('Language').value = 'en';
    target.elements.get('SmtpPassword').value = 'changed';
    target.elements.get('digestForm').handlers.submit({preventDefault() {}}); await target.flush();
    assert.equal(target.config().Digest.Language, 'en');
    await target.restore(text);
    assert.deepEqual(target.config().Digest, source.config().Digest);
    assert.equal(target.elements.get('Language').value, 'fr');
    assert.equal(target.elements.get('SmtpPassword').value, 'secret-test');
    assert.deepEqual(target.calls, ['save', 'save']);
    assert.match(target.elements.get('digestStatus').textContent, /restored and saved/);
});

test('invalid JSON, foreign backups and unsupported versions leave settings untouched', async () => {
    for (const text of ['{invalid', 'null', '{}', JSON.stringify({PluginId: 'other', FormatVersion: 1}),
        JSON.stringify({PluginId: 'f6975142-a690-4cdc-b98b-2c43b2d084ed', FormatVersion: 2, Digest: {}})]) {
        const f = fixture(); const before = structuredClone(f.config());
        await f.restore(text);
        assert.deepEqual(f.calls, []); assert.deepEqual(f.config(), before);
        assert.match(f.elements.get('digestStatus').textContent, /not valid JSON|not a supported/);
        assert.equal(f.elements.get('restoreButton').disabled, false);
    }
});

test('missing settings, invalid types and invalid ranges cannot partially overwrite configuration', async () => {
    for (const change of [options => delete options.SmtpPassword, options => {options.DeliveryEnabled = 'true';},
        options => {options.DeliveryMinute = 70;}, options => {options.Language = 'zz';},
        options => {options.SmtpPort = 587.5;}]) {
        const f = fixture(); const before = structuredClone(f.config());
        const imported = structuredClone(f.config().Digest); change(imported);
        await f.restore(backup(imported));
        assert.deepEqual(f.calls, []); assert.deepEqual(f.config(), before);
        assert.match(f.elements.get('digestStatus').textContent, /invalid/);
    }
});

test('server validation failure leaves the displayed and stored configuration unchanged', async () => {
    const f = fixture(); f.show(); await f.flush();
    const before = structuredClone(f.config());
    f.context.ApiClient.updatePluginConfiguration = async () => { throw {status: 400}; };
    const imported = structuredClone(f.config().Digest); imported.Language = 'en'; imported.TimeZoneId = 'Invalid/Zone';
    await f.restore(backup(imported));
    assert.deepEqual(f.config(), before);
    assert.equal(f.elements.get('Language').value, 'fr');
    assert.match(f.elements.get('digestStatus').textContent, /Operation failed/);
});

test('restore opens a file chooser and canceling it has no effect', () => {
    const f = fixture(); f.elements.get('restoreButton').handlers.click();
    assert.equal(f.elements.get('restoreFile').clicks, 1);
    f.elements.get('restoreFile').handlers.change();
    assert.deepEqual(f.calls, []);
});

test('oversized backups are rejected before reading or sending anything', () => {
    const f = fixture();
    f.elements.get('restoreFile').files = [{size: 65537, text() { throw new Error('Must not read this'); }}];
    f.elements.get('restoreFile').handlers.change();
    assert.match(f.elements.get('digestStatus').textContent, /exceeds 64 KB/);
    assert.deepEqual(f.calls, []);
});

test('calendar uses the server date and HTML generation submits the inclusive range once', async () => {
    const f = fixture(); await f.show();
    assert.equal(f.elements.get('HistoryFrom').value, '2026/10/03');
    f.elements.get('HistoryFrom').value = '2026-10-01';
    f.elements.get('generateReportsButton').handlers.click();
    f.elements.get('generateReportsButton').handlers.click(); await f.flush();
    assert.deepEqual(f.requests, [{route: 'LibraryHub/Reports/Generate', data: {From: '2026-10-01', To: '2026-10-03'}}]);
    assert.equal(f.calls.length, 0);
});

test('invalid, reversed and future date ranges never reach the send endpoint', async () => {
    for (const [from, to] of [['', '2026-10-03'], ['2026-02-30', '2026-10-03'],
        ['2026-10-03', '2026-10-01'], ['2026-10-03', '2026-10-04']]) {
        const f = fixture(); await f.show();
        f.elements.get('HistoryFrom').value = from; f.elements.get('HistoryTo').value = to;
        f.elements.get('generateReportsButton').handlers.click(); await f.flush();
        assert.equal(f.requests.length, 0);
        assert.match(f.elements.get('reportsStatus').textContent, /Choose valid/);
    }
});

test('HTML-only generation never calls the email endpoint and exposes a shareable archive link', async () => {
    const f = fixture(); await f.show();
    f.elements.get('HistoryFrom').value = '2026-10-01';
    f.elements.get('generateReportsButton').handlers.click(); await f.flush();
    assert.equal(f.requests.length, 1);
    assert.equal(f.requests[0].route, 'LibraryHub/Reports/Generate');
    assert.deepEqual(f.requests[0].data, {From: '2026-10-01', To: '2026-10-03'});
    assert.match(f.elements.get('reportsStatus').textContent, /2 HTML reports published. No emails were sent/);
    assert.equal(f.elements.get('PublicArchiveUrl').value, 'https://media.example.test/emby/LibraryHub/Archive');
    assert.equal(f.elements.get('generateReportsButton').disabled, false);
});


test('year-first display and calendar selection preserve exact dates for both report actions', async () => {
    for (const action of ['generateReportsButton']) {
        const f = fixture(); await f.show();
        f.elements.get('HistoryFrom').value = '2026/09/12';
        f.elements.get('HistoryToCalendar').handlers.click();
        const picker = f.elements.get('HistoryToPicker');
        assert.equal(picker.pickerOpened, true);
        assert.equal(picker.max, '2026-10-03');
        picker.value = '2026-10-02'; picker.handlers.change();
        assert.equal(f.elements.get('HistoryTo').value, '2026/10/02');
        f.elements.get(action).handlers.click();
        await f.flush();
        assert.equal(f.requests[0].data.From, '2026-09-12');
        assert.equal(f.requests[0].data.To, '2026-10-02');
    }
});

test('slash dates reject impossible, reversed and future ranges without sending requests', async () => {
    for (const [from, to] of [['2026/02/30', '2026/10/03'], ['2026/10/03', '2026/09/12'],
        ['2026/10/03', '2026/10/04'], ['10/02/2026', '2026/10/03']]) {
        const f = fixture(); await f.show();
        f.elements.get('HistoryFrom').value = from; f.elements.get('HistoryTo').value = to;
        f.elements.get('generateReportsButton').handlers.click(); await f.flush();
        assert.equal(f.requests.length, 0);
        assert.match(f.elements.get('reportsStatus').textContent, /YYYY\/MM\/DD/);
    }
});


test('old configuration exports restore with subscriptions disabled', async () => {
    const f = fixture(); const old = {...f.config().Digest}; delete old.SubscriptionsEnabled;
    await f.restore(backup(old));
    assert.deepEqual(f.calls, ['save']);
    assert.equal(f.config().Digest.SubscriptionsEnabled, false);
});

test('subscription switch saves and admin can view and remove a subscriber safely', async () => {
    const f = fixture();
    f.setSubscribers([{Id: 'b'.repeat(32), Email: '<test>@example.test', Language: 'fr', Status: 'Subscribed', Error: ''}]);
    await f.show();
    assert.equal(f.elements.has('SubscriptionUrl'), false, 'Settings should offer just the archive share link');
    assert.match(f.elements.get('subscribersStatus').textContent, /1 subscribed/);
    const row = f.elements.get('subscriberRows').children[0];
    assert.equal(row.children[0].textContent, '<test>@example.test');
    assert.equal(row.children[0].innerHTML, undefined);
    row.children[3].children[0].handlers.click(); await f.flush();
    assert.equal(f.requests[0].route, 'LibraryHub/Subscribers/Remove');
    assert.deepEqual(f.requests[0].data, {Id: 'b'.repeat(32)});
    assert.match(f.elements.get('subscribersStatus').textContent, /0 subscribed/);
    f.elements.get('SubscriptionsEnabled').checked = true;
    f.elements.get('digestForm').handlers.submit({preventDefault() {}}); await f.flush();
    assert.equal(f.config().Digest.SubscriptionsEnabled, true);
});


test('archive reset calls only its admin endpoint and keeps settings and date selections', async () => {
    const f = fixture(); await f.show();
    const before = structuredClone(f.config());
    f.elements.get('HistoryFrom').value = '2020/01/01';
    f.elements.get('resetArchiveButton').handlers.click();
    f.elements.get('resetArchiveButton').handlers.click();
    await f.flush();
    assert.deepEqual(f.requests, [{route: 'LibraryHub/Reports/Reset', data: {}}]);
    assert.deepEqual(f.config(), before); assert.deepEqual(f.calls, []);
    assert.equal(f.elements.get('HistoryFrom').value, '2020/01/01');
    assert.match(f.elements.get('archiveStatus').textContent, /268 reports removed/);
    assert.equal(f.elements.get('resetArchiveButton').disabled, false);
});

test('archive reset is blocked while generation is running', async () => {
    const f = fixture(); await f.show();
    let finish;
    f.context.ApiClient.ajax = () => new Promise(resolve => { finish = resolve; });
    f.elements.get('generateReportsButton').handlers.click();
    assert.equal(f.elements.get('resetArchiveButton').disabled, true);
    f.elements.get('resetArchiveButton').handlers.click();
    finish({Count: 1}); await f.flush();
    assert.equal(f.elements.get('resetArchiveButton').disabled, false);
});

test('legacy recipient is ignored on restore and manual email controls are absent', async () => {
    const f = fixture();
    await f.restore(backup({...f.config().Digest, Recipient: 'retired@example.test'}));
    assert.equal(f.config().Digest.Recipient, undefined);
    for (const id of ['Recipient', 'computeButton', 'cancelHistoryButton']) assert.equal(f.elements.has(id), false);
    assert.doesNotMatch(controllerScript, /LibraryDigest\/History/);
});


test('French Emby locale translates labels, help, accessible dates and messages without changing report language', async () => {
    const f = fixture('fr-FR');
    f.config().Digest.Language = 'en';
    f.setSubscribers([{Id: 'b'.repeat(32), Email: 'reader@example.test', Language: 'en', Status: 'Subscribed', Error: 'Delivery failed; retry scheduled.'}]);
    await f.show();
    assert.equal(f.page.lang, 'fr');
    const label = key => f.localized.find(n => n.getAttribute('data-digest-text') === key).textContent;
    assert.equal(label('DeliveryEnabled'), 'Activer les courriels quotidiens aux abonnés');
    assert.equal(label('Save'), 'Enregistrer');
    assert.equal(label('SmtpPassword'), 'Mot de passe SMTP');
    assert.equal(f.elements.get('HistoryFrom').getAttribute('placeholder'), 'AAAA/MM/JJ');
    assert.equal(f.elements.get('HistoryFrom').getAttribute('aria-label'), 'Date de début, AAAA/MM/JJ');
    assert.equal(f.elements.get('Language').value, 'en');
    const row = f.elements.get('subscriberRows').children[0];
    assert.match(row.children[2].textContent, /^Abonné — Échec de l’envoi/);
    assert.equal(row.children[3].children[0].textContent, 'Désabonner');
    f.elements.get('digestForm').handlers.submit({preventDefault() {}}); await f.flush();
    assert.match(f.elements.get('digestStatus').textContent, /^Paramètres enregistrés/);
    assert.equal(f.config().Digest.Language, 'en');
    await f.restore('{bad');
    assert.match(f.elements.get('digestStatus').textContent, /JSON n’est pas valide/);
    f.elements.get('HistoryFrom').value = '2026/02/30';
    f.elements.get('generateReportsButton').handlers.click();
    assert.match(f.elements.get('reportsStatus').textContent, /AAAA\/MM\/JJ/);
    f.elements.get('HistoryFrom').value = '2026/10/01';
    f.elements.get('generateReportsButton').handlers.click(); await f.flush();
    assert.match(f.elements.get('reportsStatus').textContent, /^2 rapports HTML publiés/);
    assert.equal(f.requests[0].data.From, '2026-10-01');
});

test('English and unsupported Emby locales use English and a changed preference is applied when reopening', async () => {
    const f = fixture('fr_CA'); await f.show(); assert.equal(f.page.lang, 'fr');
    for (const locale of ['en-GB', 'de-DE']) {
        f.setUiLocale(locale); await f.show();
        assert.equal(f.page.lang, 'en');
        assert.equal(f.localized.find(n => n.getAttribute('data-digest-text') === 'Save').textContent, 'Save');
    }
});

test('French server generation errors remain actionable and translated', async () => {
    const f = fixture('fr'); await f.show();
    f.context.ApiClient.ajax = async () => ({Error: 'Emby is starting or scanning. Try generation again when it finishes.'});
    f.elements.get('generateReportsButton').handlers.click(); await f.flush();
    assert.match(f.elements.get('reportsStatus').textContent, /Emby démarre ou analyse/);
});

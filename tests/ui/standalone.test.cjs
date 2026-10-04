const {test} = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const {webcrypto} = require('node:crypto');
const html = fs.readFileSync(path.join(__dirname, '../../src/Emby.LibraryHub/Standalone/page.html'), 'utf8');
const script = fs.readFileSync(path.join(__dirname, '../../src/Emby.LibraryHub/Standalone/app.js'), 'utf8');
function fixture({pathname = '/proxy/emby/LibraryHub/Archive/2026-10-03', search = '', token = '', reportStatus = 200, deferred = false} = {}) {
    const makeElement = () => ({value: '', hidden: true, textContent: '', handlers: {}, children: [], srcdoc: '',
        addEventListener(type, handler) { this.handlers[type] = handler; }, appendChild(child) { this.children.push(child); }});
    const elements = new Map([...html.matchAll(/id="([^"]+)"/g)].map(([,id]) => [id, makeElement()]));
    const labels = [...html.matchAll(/data-text="([^"]+)"/g)].map(([,key]) => ({textContent: '', getAttribute: () => key}));
    const storage = new Map(token ? [['library-hub:/proxy/emby:token', token]] : []);
    const calls = [], events = {}, location = {pathname, search}, document = {documentElement: {}, getElementById: id => elements.get(id),
        querySelectorAll: () => labels, createElement: makeElement};
    let releaseReport;
    const reply = (body, status=200) => ({ok: status >= 200 && status < 300, status, text: async () => JSON.stringify(body)});
    const context = {document, location, navigator: {language: 'fr-FR'}, crypto: webcrypto, Uint8Array, URLSearchParams, AbortController, setTimeout, clearTimeout,
        DOMParser: class {
            parseFromString(source) {
                return {querySelectorAll: () => [], documentElement: {outerHTML: '<html><head></head><body>' + source + '</body></html>'}};
            }
        },
        addEventListener: (name, handler) => { events[name] = handler; },
        sessionStorage: {getItem: key => storage.get(key), setItem: (key, value) => storage.set(key,value), removeItem: key => storage.delete(key)},
        fetch: async (url, options) => {
            calls.push({url, ...options});
            if (url.endsWith('Users/AuthenticateByName')) return reply({AccessToken: 'session-token', User: {Id: 'account-7'}});
            if (url.includes('Community/Reports')) {
                if (deferred) return new Promise(resolve => { releaseReport = () => resolve(reply({Html: '<h1>Late saved report</h1>'})); });
                return reply({Html: '<h1>Saved daily report</h1>'}, reportStatus);
            }
            if (options.method === 'POST') return reply({Success: true});
            return reply({Enabled: true, FormToken: 'account-proof', Subscriptions: [{Id: 'mine', Email: '<reader>@example.test', Status: 'Subscribed'}]});
        }
    };
    context.window = context;
    const persistent = new Map();
    context.localStorage = {getItem: key => persistent.get(key), setItem: (key, value) => persistent.set(key,value), removeItem: key => persistent.delete(key)};
    vm.runInNewContext(script, context);
    return {elements, calls, events, storage, persistent, context, labels, release: () => releaseReport(), flush: () => new Promise(resolve => setImmediate(resolve))};
}
test('standalone permalink shows login first, then the same saved daily report without navigating to Emby', async () => {
    const f = fixture(); await f.flush();
    assert.equal(f.calls.length, 0); assert.equal(f.elements.get('loginPanel').hidden, false);
    f.elements.get('username').value = 'member'; f.elements.get('password').value = 'test-password';
    await f.elements.get('loginForm').handlers.submit({preventDefault(){}});
    assert.equal(f.context.location.pathname, '/proxy/emby/LibraryHub/Archive/2026-10-03');
    assert.equal(f.calls[0].url, '/proxy/emby/Users/AuthenticateByName');
    assert.deepEqual(JSON.parse(f.calls[0].body), {Username: 'member', Pw: 'test-password'});
    assert.equal(f.calls[1].url, '/proxy/emby/LibraryHub/Community/Reports?Day=2026-10-03');
    assert.equal(f.calls[1].headers['X-Emby-Token'], 'session-token');
    assert.equal(f.elements.get('archive').srcdoc, '<!doctype html><html><head></head><body><h1>Saved daily report</h1></body></html>');
    assert.equal(f.elements.get('archive').hidden, false); assert.equal(f.elements.get('password').value, '');
    assert.equal(f.elements.get('subscriptionPanel').hidden, true);
    assert.doesNotMatch(f.calls.map(c => c.url).join('\n'), /token|configurationpage|Generate|Preview/);
    assert.equal(f.context.document.documentElement.lang, 'fr');
});
test('existing tab session opens archive without asking for credentials again', async () => {
    const f = fixture({token: 'saved-token'}); await f.flush();
    assert.equal(f.calls.length, 1); assert.equal(f.elements.get('loginPanel').hidden, true);
    assert.equal(f.calls[0].headers['X-Emby-Token'], 'saved-token');
});
test('expired or revoked sessions clear report content and return to login', async () => {
    const f = fixture({token: 'old-token', reportStatus: 401}); await f.flush();
    assert.equal(f.elements.get('archive').srcdoc, ''); assert.equal(f.elements.get('archive').hidden, true);
    assert.equal(f.storage.has('library-hub:/proxy/emby:token'), false);
    assert.equal(f.elements.get('loginPanel').hidden, false);
});
test('logout revokes the Emby session and late responses cannot reveal the report', async () => {
    const f = fixture({token: 'saved-token', deferred: true}); await f.flush();
    await f.elements.get('logout').handlers.click(); f.release(); await f.flush();
    assert.equal(f.calls[1].url, '/proxy/emby/Sessions/Logout');
    assert.equal(f.calls[1].headers['X-Emby-Token'], 'saved-token');
    assert.equal(f.elements.get('archive').srcdoc, ''); assert.equal(f.elements.get('loginPanel').hidden, false);
});
test('standalone subscription page fetches only own membership and never loads archive content', async () => {
    const f = fixture({pathname: '/proxy/emby/LibraryHub/Subscriptions', token: 'saved-token'}); await f.flush();
    assert.equal(f.calls.length, 1); assert.match(f.calls[0].url, /Community\/Subscription\?/);
    assert.equal(f.elements.get('subscriptionPanel').hidden, false); assert.equal(f.elements.get('archive').hidden, true);
    assert.equal(f.elements.get('membershipRows').children[0].children[0].textContent, '<reader>@example.test — Abonné');
    f.elements.get('email').value = 'me@example.test'; f.elements.get('mailLanguage').value = 'fr';
    await f.elements.get('subscribeForm').handlers.submit({preventDefault(){}});
    assert.deepEqual(JSON.parse(f.calls.at(-1).body), {Action: 'subscribe', FormToken: 'account-proof', Email: 'me@example.test', Language: 'fr'});
});
test('confirmation links survive login but never subscribe without clicking confirm', async () => {
    const f = fixture({pathname: '/proxy/emby/LibraryHub/Subscriptions', token: 'saved-token', search: '?action=confirm&token=' + 'b'.repeat(64)}); await f.flush();
    assert.equal(f.calls.filter(c => c.method === 'POST').length, 0);
    await f.elements.get('confirmButton').handlers.click();
    assert.deepEqual(JSON.parse(f.calls.find(c => c.method === 'POST').body), {Action: 'confirm', Token: 'b'.repeat(64), FormToken: 'account-proof'});
    assert.equal(f.elements.get('confirmButton').hidden, true);
});
test('leaving the page clears private HTML and email data', async () => {
    const f = fixture({token: 'saved-token'}); await f.flush(); f.events.pagehide();
    assert.equal(f.elements.get('archive').srcdoc, ''); assert.equal(f.elements.get('archive').hidden, true);
    assert.equal(f.elements.get('email').value, '');
    assert.match(html, /sandbox="allow-top-navigation-by-user-activation"/);
    assert.doesNotMatch(html, /allow-scripts|allow-same-origin|configurationpage/);
});

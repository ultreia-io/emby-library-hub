const {test} = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const {loadLocale, localizedPage} = require('./locale-fixture.cjs');
const configDir = path.join(__dirname, '../../src/Emby.LibraryHub/Configuration');

test('all settings and archive labels have French and English translations', async () => {
    for (const file of ['config.html', 'reports.html', 'subscriptions.html']) {
        const page = {};
        const html = fs.readFileSync(path.join(configDir, file), 'utf8');
        const nodes = localizedPage(page, html);
        const locale = loadLocale({Emby: {importModule: async () => ({default: {getCurrentLocale: () => 'fr-FR'}})}}).create(page);
        await locale.load();
        for (const node of nodes) {
            for (const attr of ['text', 'aria', 'placeholder', 'title']) {
                const key = node.getAttribute('data-digest-' + attr);
                if (key && key !== 'Action') assert.notEqual(locale.text(key), key, 'Missing French translation for ' + key);
            }
        }
        // Every human-readable text leaf must be marked, except brand names and language autonyms.
        for (const [, tag, attrs, text] of html.matchAll(/<([a-z][a-z0-9]*)([^<>]*)>([^<>]+)<\/\1>/g)) {
            if (['Library Hub', 'ultreia.io', 'Français', 'English'].includes(text.trim()) || !text.trim()) continue;
            assert.match(attrs, /data-digest-text=/, `${file}: untranslated ${tag}: ${text}`);
        }
    }
});

test('Emby preference takes priority over browser language; unavailable module uses server culture then browser', async () => {
    const environment = {document: {documentElement: {getAttribute: () => 'fr-FR', lang: ''}}, navigator: {language: 'en-US'}};
    const page = {}; localizedPage(page, '');
    const locale = loadLocale({...environment, Emby: {importModule: async () => ({getCurrentLocale: () => 'en-US'})}}).create(page);
    await locale.load(); assert.equal(page.lang, 'en');
    const fallback = loadLocale({...environment, Emby: {importModule: async () => { throw new Error('Unavailable'); }}}).create(page);
    await fallback.load(); assert.equal(page.lang, 'fr');
    const browser = loadLocale({navigator: {language: 'fr-CA'}}).create(page);
    await browser.load(); assert.equal(page.lang, 'fr');
    const unknown = loadLocale({navigator: {language: 'ja'}}).create(page);
    await unknown.load(); assert.equal(page.lang, 'en');
});

const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const source = fs.readFileSync(path.join(__dirname, '../../src/Emby.LibraryHub/Configuration/locale.js'), 'utf8');
function loadLocale(context = {}) {
    let locale;
    vm.runInNewContext(source, {...context, define(deps, factory) { locale = factory(); }});
    return locale;
}
function localizedPage(page, html, elements = new Map()) {
    const nodes = [...html.matchAll(/<[^<>]+data-digest-(?:text|aria|placeholder|title)="[^"]+"[^<>]*>/g)].map(([tag]) => {
        const attrs = Object.fromEntries([...tag.matchAll(/([\w-]+)="([^"]*)"/g)].map(([, name, value]) => [name, value]));
        const node = elements.get(attrs.id) || {};
        node.getAttribute = name => attrs[name];
        node.setAttribute = (name, value) => { attrs[name] = value; };
        return node;
    });
    page.setAttribute = (name, value) => { page[name] = value; };
    page.querySelectorAll = selector => nodes.filter(node => node.getAttribute(selector.slice(1, -1)) !== undefined);
    return nodes;
}
module.exports = {loadLocale, localizedPage};

const {test} = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
function redirect(page, params) {
    let controller, target;
    vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/Emby.LibraryHub/Configuration/' + page + '.js'), 'utf8'), {
        ApiClient: {getUrl: route => 'https://media.test/proxy/emby/' + route},
        window: {location: {replace: value => { target = value; }}},
        define(deps, factory) {
            function BaseView(view, params) { this.params = params; }
            BaseView.prototype.onResume = function () {};
            const View = factory(BaseView); controller = new View({}, params);
        }
    });
    controller.onResume(); return target;
}
test('Emby menu and old archive links open the standalone website without fetching reports', () => {
    assert.equal(redirect('reports', {}), 'https://media.test/proxy/emby/LibraryHub/Archive');
    assert.equal(redirect('reports', {day: '2026-10-03'}), 'https://media.test/proxy/emby/LibraryHub/Archive/2026-10-03');
    assert.equal(redirect('subscriptions', {}), 'https://media.test/proxy/emby/LibraryHub/Subscriptions');
});
test('old confirmation links preserve their action but never perform it automatically', () => {
    assert.equal(redirect('reports', {action: 'confirm', token: 'b'.repeat(64), lang: 'fr'}),
        'https://media.test/proxy/emby/LibraryHub/Subscriptions?action=confirm&token=' + 'b'.repeat(64) + '&lang=fr');
});
test('legacy redirects reject injected paths and unsafe parameters', () => {
    assert.equal(redirect('reports', {day: '../../admin', action: '<script>'}), 'https://media.test/proxy/emby/LibraryHub/Archive');
});

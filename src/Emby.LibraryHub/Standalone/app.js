(function () {
  'use strict';
  var root = location.pathname.substring(0, location.pathname.toLowerCase().lastIndexOf('/libraryhub/'));
  var apiRoot = root + '/', siteRoot = root + '/LibraryHub';
  var searching = /\/Search\/?$/i.test(location.pathname);
  var browsing = searching || /\/Browse\/?$/i.test(location.pathname), browseUI;
  var subscriptions = /\/Subscriptions\/?$/i.test(location.pathname);
  var match = location.pathname.match(/\/Archive\/([0-9]{4}-[0-9]{2}-[0-9]{2})\/?$/i);
  var day = match ? match[1] : '';
  var query = new URLSearchParams(location.search), action = query.get('action') || 'subscribe';
  if (['subscribe', 'confirm', 'unsubscribe'].indexOf(action) < 0) action = 'subscribe';
  var actionView = subscriptions && action !== 'subscribe', completed = false;
  var actionToken = /^[a-f0-9]{64}$/i.test(query.get('token') || '') ? query.get('token') : '';
  var key = 'library-hub:' + root + ':', token = '', tokenSource = '', epoch = 0, proof = '';
  var language = query.get('lang') || read('language') || navigator.language || 'en';
  language = language.toLowerCase().startsWith('fr') ? 'fr' : 'en';
  var device = read('device');
  if (!/^[a-f0-9]{32}$/.test(device)) {
    device = Array.from(crypto.getRandomValues(new Uint8Array(16)), function (b) { return b.toString(16).padStart(2, '0'); }).join('');
    save('device', device);
  }
  var clientHeaders = {'X-Emby-Client': 'Library Hub', 'X-Emby-Device-Name': 'Browser',
    'X-Emby-Device-Id': device, 'X-Emby-Client-Version': '0.1.0'};
  var messages = {
    browse: ['Browse', 'Explorer'], search: ['Search', 'Rechercher'],
    browseLoginHelp: ['Sign in to browse your libraries.', 'Connectez-vous pour explorer vos médiathèques.'],
    archive: ['Archive', 'Archives'], subscription: ['My subscription', 'Mon abonnement'], logout: ['Sign out', 'Déconnexion'],
    signin: ['Sign in with your Emby account', 'Connexion avec votre compte Emby'],
    loginHelp: ['Sign in to read the saved reports. You will stay on this page.', 'Connectez-vous pour lire les rapports enregistrés. Vous resterez sur cette page.'],
    remember: ['Remember me on this browser', 'Rester connecté sur ce navigateur'],
    username: ['Username', 'Nom d’utilisateur'], password: ['Password', 'Mot de passe'], signinButton: ['Sign in', 'Connexion'],
    email: ['Email address', 'Adresse électronique'], mailLanguage: ['Email language', 'Langue des courriels'],
    subscribe: ['Send confirmation email', 'Envoyer le courriel de confirmation'], loading: ['Loading…', 'Chargement…'],
    loginFailed: ['Sign-in failed. Check your Emby username and password.', 'Échec de connexion. Vérifiez vos identifiants Emby.'],
    expired: ['Please sign in to continue.', 'Connectez-vous pour continuer.'], unavailable: ['Unable to load this page. Please try again.', 'Impossible de charger cette page. Réessayez.'],
    missing: ['This report has not been published.', 'Ce rapport n’a pas été publié.'],
    confirmHelp: ['Click below to confirm your email subscription.', 'Cliquez ci-dessous pour confirmer votre abonnement aux courriels.'],
    unsubscribeHelp: ['Click below to stop receiving daily emails.', 'Cliquez ci-dessous pour ne plus recevoir les courriels quotidiens.'],
    actionLoginHelp: ['Sign in to continue. You can then confirm your choice.', 'Connectez-vous pour continuer. Vous pourrez ensuite confirmer votre choix.'],
    confirm: ['Confirm my subscription', 'Confirmer mon abonnement'], unsubscribe: ['Unsubscribe', 'Me désabonner'],
    confirmUnsubscribe: ['Confirm unsubscribe', 'Confirmer mon désabonnement'],
    requested: ['Check your email and use the latest confirmation link. Previous links have been replaced.', 'Consultez votre messagerie et utilisez le dernier lien de confirmation. Les liens précédents ont été remplacés.'],
    addressInUse: ['This email address belongs to another Emby account. Sign in with that account or contact the administrator.', 'Cette adresse est liée à un autre compte Emby. Connectez-vous avec ce compte ou contactez l’administrateur.'],
    invalidEmail: ['Enter one valid email address.', 'Saisissez une adresse électronique valide.'],
    invalidLanguage: ['Choose French or English.', 'Choisissez le français ou l’anglais.'],
    expiredForm: ['This form expired. Reload the page and try again.', 'Ce formulaire a expiré. Rechargez la page et réessayez.'],
    requestFailed: ['The confirmation email could not be sent. Please contact the server administrator.', 'Le courriel de confirmation n’a pas pu être envoyé. Contactez l’administrateur du serveur.'],
    confirmed: ['Your subscription is confirmed.', 'Votre abonnement est confirmé.'], removed: ['You are unsubscribed.', 'Vous êtes désabonné.'],
    invalid: ['This link is invalid or expired, or belongs to another account.', 'Ce lien est invalide, expiré ou associé à un autre compte.'],
    paused: ['New subscriptions are currently paused.', 'Les nouvelles inscriptions sont suspendues.'],
    subscribed: ['Subscribed', 'Abonné'], pending: ['Awaiting confirmation', 'En attente de confirmation']
  };
  function el(id) { return document.getElementById(id); }
  function text(id) { return messages[id][language === 'fr' ? 1 : 0]; }
  function status(id) {
    el('status').textContent = ''; el('actionStatus').textContent = '';
    el(actionView && !el('actionPanel').hidden ? 'actionStatus' : 'status').textContent = id ? text(id) : '';
  }
  function stored(area, name) { try { return window[area].getItem(name) || ''; } catch (_) { return ''; } }
  function write(area, name, value) {
    try { if (value) window[area].setItem(name, value); else window[area].removeItem(name); } catch (_) {}
  }
  function read(name) { return stored('localStorage', key + name) || stored('sessionStorage', key + name); }
  function save(name, value) {
    write('localStorage', key + name, value);
    write('sessionStorage', key + name, '');
    if (value && stored('localStorage', key + name) !== value) write('sessionStorage', key + name, value);
  }
  function saveToken(value) {
    write('localStorage', key + 'token', ''); write('sessionStorage', key + 'token', '');
    if (!value) return;
    if (el('rememberLogin').checked) save('token', value);
    else write('sessionStorage', key + 'token', value);
  }
  async function resume() {
    clear(); var current = epoch, previous = token;
    token = read('token'); tokenSource = token ? 'digest' : '';
    if (token && read('remember') !== 'false') save('token', token);
    if (!token && !read('signedOut')) {
      try {
        var credentials = JSON.parse(stored('localStorage', 'servercredentials3') || '{}');
        if (Array.isArray(credentials.Servers) && credentials.Servers.length) {
          status('loading');
          var server = await request('System/Info/Public', undefined, true);
          if (current !== epoch) return;
          // Match the server ID, then only its currently selected user, never another saved account.
          var entry = credentials.Servers.find(function (item) { return item.Id === server.Id; });
          if (entry && entry.UserId) {
            var user = (entry.Users || []).find(function (item) { return item.UserId === entry.UserId; });
            token = (user ? user.AccessToken : entry.AccessToken) || '';
            tokenSource = token ? 'emby' : '';
          }
        }
      } catch (_) { /* No usable Emby browser session: the normal login remains available. */ }
    }
    if (current !== epoch) return;
    if (token !== previous) completed = false;
    await load();
  }
  function translate() {
    document.documentElement.lang = language; el('uiLanguage').value = language;
    document.querySelectorAll('[data-text]').forEach(function (node) { node.textContent = text(node.getAttribute('data-text')); });
    el('archive').title = text('archive');
    el('confirmButton').textContent = text(action === 'confirm' ? 'confirm' : 'confirmUnsubscribe');
    el('actionTitle').textContent = el('confirmButton').textContent;
    el('actionHelp').textContent = text(action === 'confirm' ? 'confirmHelp' : 'unsubscribeHelp');
    if (browsing) el('loginHelp').textContent = text('browseLoginHelp');
    if (actionView) el('loginHelp').textContent = text('actionLoginHelp');
  }
  function clear() {
    if (browseUI) browseUI.clear();
    epoch++; proof = ''; el('actionPanel').hidden = true; el('confirmButton').hidden = true; el('actionStatus').textContent = ''; el('archive').srcdoc = ''; el('archive').hidden = true;
    el('subscriptionPanel').hidden = true; el('membershipRows').textContent = ''; el('email').value = '';
  }
  function login(message) {
    clear(); completed = false; token = ''; tokenSource = ''; saveToken(''); el('logout').hidden = true; el('loginPanel').hidden = false; status(message || 'expired');
  }
  async function request(path, data, anonymous, signal) {
    var headers = Object.assign({'Accept': 'application/json', 'X-Emby-Language': language}, clientHeaders);
    if (!anonymous && token) headers['X-Emby-Token'] = token;
    if (data !== undefined) headers['Content-Type'] = 'application/json';
    var controller = new AbortController(), timer = setTimeout(function () { controller.abort(); }, 20000);
    function abort() { controller.abort(); }
    if (signal) { if (signal.aborted) abort(); else signal.addEventListener('abort', abort, {once: true}); }
    try {
    var response = await fetch(apiRoot + path, {method: data === undefined ? 'GET' : 'POST', headers: headers,
      body: data === undefined ? undefined : JSON.stringify(data), cache: 'no-store', credentials: 'omit', signal: controller.signal});
    if (!response.ok) { var error = new Error('Request failed'); error.status = response.status; throw error; }
    if (response.status === 204) return {Success: true};
    var body = await response.text(); return body ? JSON.parse(body) : {};
    } finally { clearTimeout(timer); if (signal) signal.removeEventListener('abort', abort); }
  }
  function fail(error, current) {
    if (current !== epoch) return;
    if (error.status === 401 || error.status === 403) { save('signedOut', String(Date.now())); login('expired'); }
    else status(error.status === 404 ? 'missing' : 'unavailable');
  }
  async function load() {
    clear(); var current = epoch;
    if (!token) { login(); return; }
    el('loginPanel').hidden = true; el('logout').hidden = false;
    if (actionView) {
      el('actionPanel').hidden = false; el('actionHelp').hidden = completed;
      if (completed) { status(action === 'confirm' ? 'confirmed' : 'removed'); return; }
      if (!actionToken) { status('invalid'); return; }
    }
    status('loading');
    try {
      if (browsing) { await browseUI.load(); if (current === epoch) status(''); }
      else if (!subscriptions) {
        var report = await request('LibraryHub/Community/Reports?Day=' + encodeURIComponent(day));
        if (current !== epoch) return;
        // The surrounding page owns branding; saved standalone reports keep their own footer.
        var reportDocument = new DOMParser().parseFromString(report.Html, 'text/html');
        reportDocument.querySelectorAll('.hub-footer').forEach(function (footer) { footer.remove(); });
        el('archive').srcdoc = '<!doctype html>' + reportDocument.documentElement.outerHTML;
        el('archive').hidden = false; status('');
      } else {
        var result = await request('LibraryHub/Community/Subscription?Action=' + action + '&Token=' + encodeURIComponent(actionToken));
        if (current !== epoch) return;
        proof = result.FormToken;
        if (actionView) { el('confirmButton').hidden = false; status(''); return; }
        el('subscriptionPanel').hidden = false; el('subscribeButton').disabled = !result.Enabled;
        el('confirmButton').hidden = action === 'subscribe' || !actionToken;
        status(result.Enabled ? '' : 'paused');
        result.Subscriptions.forEach(function (subscription) {
          var row = document.createElement('p'), label = document.createElement('span'), button = document.createElement('button');
          label.textContent = subscription.Email + ' — ' + text(subscription.Status === 'Subscribed' ? 'subscribed' : 'pending');
          button.type = 'button'; button.textContent = text('unsubscribe');
          button.addEventListener('click', async function () {
            button.disabled = true;
            try { await request('LibraryHub/Community/Subscription/Remove', {Id: subscription.Id}); if (current === epoch) await load(); }
            catch (error) { fail(error, current); button.disabled = false; }
          });
          row.appendChild(label); row.appendChild(button); el('membershipRows').appendChild(row);
        });
      }
    } catch (error) { fail(error, current); }
  }
  el('loginForm').addEventListener('submit', async function (event) {
    event.preventDefault(); var current = epoch; el('loginButton').disabled = true; status('loading');
    try {
      var result = await request('Users/AuthenticateByName', {Username: el('username').value, Pw: el('password').value}, true);
      if (current !== epoch) return;
      if (!result.AccessToken || !result.User || !result.User.Id) throw new Error('No user session');
      token = result.AccessToken; tokenSource = 'digest'; completed = false;
      save('remember', el('rememberLogin').checked ? 'true' : 'false');
      save('signedOut', ''); saveToken(token); await load();
    } catch (_) { if (current === epoch) status('loginFailed'); }
    finally { el('password').value = ''; el('loginButton').disabled = false; }
  });
  el('logout').addEventListener('click', async function () {
    var pending = request('Sessions/Logout', {}); save('signedOut', String(Date.now())); login();
    try { await pending; } catch (_) { /* Local session is already cleared. */ }
  });
  el('subscribeForm').addEventListener('submit', async function (event) {
    event.preventDefault(); var current = epoch; el('subscribeButton').disabled = true;
    try {
      var fresh = await request('LibraryHub/Community/Subscription?Action=subscribe');
      if (current !== epoch) return;
      var result = await request('LibraryHub/Community/Subscription', {Action: 'subscribe', FormToken: fresh.FormToken, Email: el('email').value, Language: el('mailLanguage').value});
      if (current === epoch) status(result.Success ? 'requested' :
        (['addressInUse', 'invalidEmail', 'invalidLanguage', 'expiredForm', 'paused'].indexOf(result.ErrorCode) >= 0 ? result.ErrorCode : 'requestFailed'));
    } catch (error) { fail(error, current); }
    finally { el('subscribeButton').disabled = false; }
  });
  el('confirmButton').addEventListener('click', async function () {
    if (!proof) return; var current = epoch; el('confirmButton').disabled = true;
    try {
      var result = await request('LibraryHub/Community/Subscription', {Action: action, Token: actionToken, FormToken: proof});
      if (current !== epoch) return;
      if (result.Success) { completed = true; el('confirmButton').hidden = true; el('actionHelp').hidden = true; status(action === 'confirm' ? 'confirmed' : 'removed'); }
      else status('invalid');
    } catch (error) { fail(error, current); }
    finally { el('confirmButton').disabled = false; }
  });
  el('uiLanguage').addEventListener('change', function () { language = el('uiLanguage').value === 'fr' ? 'fr' : 'en'; save('language', language); translate(); load(); });
  addEventListener('pagehide', clear);
  addEventListener('pageshow', function (event) { if (event.persisted) resume(); });
  addEventListener('storage', function (event) {
    if (event.key === key + 'signedOut' && event.newValue) { login(); return; }
    if (event.key === key + 'token' || event.key === null ||
        (event.key === 'servercredentials3' && tokenSource !== 'digest')) resume();
  });
  if (browsing) browseUI = window.LibraryHubBrowse({searchMode: searching, el: el, request: request, read: read, save: save, language: function () { return language; },
    webRoot: root.replace(/\/emby$/i, ''), fail: function (error) { fail(error, epoch); }});
  el('browseLink').href = siteRoot + '/Browse';
  el('searchLink').href = siteRoot + '/Search';
  if (browsing) el(searching ? 'searchLink' : 'browseLink').setAttribute('aria-current', 'page');
  el('archiveLink').href = siteRoot + '/Archive'; el('subscriptionLink').href = siteRoot + '/Subscriptions';
  el('rememberLogin').checked = read('remember') !== 'false';
  el('mailLanguage').value = language; translate(); resume();
})();

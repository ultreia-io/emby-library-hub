(function () {
  'use strict';
  window.LibraryHubBrowse = function (context) {
    var el = context.el, api = context.request, sequence = 0, controller, timer, catalog, chosen = new Set(), includeEpisodeMetadata = false;
    var searchMode = !!context.searchMode, rootGroups = [], seasonViews = [];
    el('browseShowEpisodes').checked = false;
    if (!searchMode) {
      el('browseControls').hidden = false;
      el('browseControls').appendChild(el('libraryControls'));
      el('browseControls').appendChild(el('catalogOrdering'));
    }
    var labels = {
      browse: ['Browse', 'Explorer'], searchPage: ['Search', 'Rechercher'],
      intro: ['Explore your libraries by opening groups and titles.', 'Explorez vos médiathèques en dépliant les groupes et les titres.'],
      searchIntro: ['Choose metadata and refine your search across libraries.', 'Choisissez les métadonnées et affinez votre recherche dans les médiathèques.'],
      searchIn: ['Search in', 'Rechercher dans'],
      Title: ['Title', 'Titre'], OriginalTitle: ['Original title', 'Titre original'], AllMetadata: ['All metadata', 'Toutes les métadonnées'],
      Overview: ['Synopsis', 'Résumé'], Actors: ['Actors', 'Interprètes'], Directors: ['Directors', 'Réalisation'],
      Genres: ['Genres', 'Genres'], Tags: ['Tags', 'Étiquettes'], Studios: ['Studios', 'Studios'], Year: ['Year', 'Année'],
      searchPrompt: ['Enter search text or choose a criterion.', 'Saisissez une recherche ou choisissez un critère.'],
      searching: ['Searching…', 'Recherche en cours…'],
      shortSearch: ['Enter at least 2 characters to search metadata.', 'Saisissez au moins 2 caractères pour rechercher dans les métadonnées.'],
      libraries: ['Libraries', 'Médiathèques'], all: ['Select all', 'Tout sélectionner'], none: ['Clear selection', 'Tout désélectionner'],
      showEpisodes: ['Show episodes', 'Afficher les épisodes'],
      episodeMetadata: ['Also search episode metadata', 'Rechercher aussi dans les métadonnées des épisodes'],
      search: ['Search text (at least 2 characters)', 'Texte à rechercher (2 caractères minimum)'], searchHint: ['Enter search text…', 'Saisissez votre recherche…'], kind: ['Media type', 'Type de média'], any: ['All', 'Tous'],
      genre: ['Genre', 'Genre'], genreHint: ['Exact genre, e.g. Animation', 'Genre exact, ex. Animation'], year: ['Year', 'Année'], played: ['Watched / played', 'Vu / écouté'],
      yes: ['Watched / played', 'Vu / écouté'], no: ['Unwatched / unplayed', 'Non vu / non écouté'], favorites: ['Favourites only', 'Favoris uniquement'],
      sort: ['Sort by', 'Trier par'], SortName: ['Title', 'Titre'], DateCreated: ['Date added', 'Date d’ajout'], PremiereDate: ['Release date', 'Date de sortie'],
      ProductionYear: ['Year', 'Année'], CommunityRating: ['Rating', 'Note'], Director: ['Director', 'Réalisation'], Actor: ['Actor', 'Interprètes'], order: ['Order', 'Ordre'], ascending: ['Ascending', 'Croissant'], descending: ['Descending', 'Décroissant'],
      apply: ['Search', 'Rechercher'], reset: ['Clear', 'Effacer'], expand: ['Expand one level', 'Déplier un niveau'], collapse: ['Collapse all', 'Tout replier'],
      choose: ['Select at least one library to start browsing.', 'Sélectionnez au moins une médiathèque pour commencer.'], empty: ['No matching items.', 'Aucun élément correspondant.'],
      unavailable: ['This library could not be loaded.', 'Impossible de charger cette médiathèque.'], retry: ['Try again', 'Réessayer'], loading: ['Loading…', 'Chargement…'], more: ['Load more', 'Afficher la suite'],
      shown: ['items shown', 'éléments affichés'], shownOne: ['item shown', 'élément affiché'], noLibraries: ['No libraries are available for your account.', 'Aucune médiathèque n’est accessible à votre compte.'],
      unknown: ['Unknown', 'Inconnu'], other: ['Other', 'Autres'], season: ['Season', 'Saison'], specials: ['Specials', 'Hors-série'], openItem: ['Open in Emby', 'Ouvrir dans Emby'], groupsShown: ['groups shown', 'groupes affichés'],
      collectionHint: ['Expand a collection to browse its titles.', 'Dépliez une collection pour explorer ses titres.'],
      Library: ['Library', 'Médiathèque'], BoxSet: ['Collection', 'Collection'], Movie: ['Film', 'Film'], Series: ['Series', 'Série'], Episode: ['Episode', 'Épisode'], Audio: ['Music', 'Musique'], MusicVideo: ['Music video', 'Clip musical'], Video: ['Video', 'Vidéo'], AudioBook: ['Audiobook', 'Livre audio'], Photo: ['Photo', 'Photo']
    };
    function t(name) { return labels[name] ? labels[name][context.language() === 'fr' ? 1 : 0] : name; }
    function node(tag, value, className) { var n = document.createElement(tag); if (value) n.textContent = value; if (className) n.className = className; return n; }
    function translate() {
      el('catalogHeading').textContent = t(searchMode ? 'searchPage' : 'browse');
      el('catalogIntro').textContent = t(searchMode ? 'searchIntro' : 'intro');
      el('browseForm').hidden = !searchMode;
      el('browsePanel').querySelectorAll('[data-browse]').forEach(function (n) { n.textContent = t(n.getAttribute('data-browse')); });
      el('browseSearch').placeholder = t('searchHint'); el('browseGenre').placeholder = t('genreHint');
    }
    function libraryLabel(item, checked, onChange) {
      var label = node('label', '', 'library-choice'), input = node('input'); input.type = 'checkbox'; input.value = item.Id; input.checked = checked;
      input.addEventListener('change', function () { onChange(input.checked); });
      label.appendChild(input); label.appendChild(node('span', item.Name)); return label;
    }
    function selection() {
      el(searchMode ? 'librarySummary' : 'browseLibrariesHeading').textContent = t('libraries') + ' · ' + chosen.size;
      context.save('browseLibraries', JSON.stringify(Array.from(chosen)));
    }
    function renderChoices() {
      el('libraryChoices').textContent = '';
      catalog.Libraries.forEach(function (c) {
        var label = libraryLabel(c, chosen.has(c.Id), function (checked) { if (checked) chosen.add(c.Id); else chosen.delete(c.Id); selection(); search(); });
        el('libraryChoices').appendChild(label);
      });
      selection();
    }
    function filters() {
      var ordering = {Sort: el('browseSort').value, Descending: el('browseOrder').value === 'desc'};
      if (!searchMode) return Object.assign({Search: '', IncludeEpisodeMetadata: false, Kind: '', Genre: '', Year: '', Played: '', Favorites: false}, ordering);
      if (el('browseSearch').value.trim().length === 1) { el('browseStatus').textContent = t('shortSearch'); return null; }
      var year = el('browseYear').value.trim();
      if (year && (!/^\d{1,4}$/.test(year) || Number(year) < 1)) { el('browseYear').reportValidity(); return null; }
      return { Search: el('browseSearch').value.trim(), SearchField: el('browseSearchField').value, IncludeEpisodeMetadata: el('browseEpisodeMetadata').checked, Kind: el('browseKind').value, Genre: el('browseGenre').value.trim(), Year: year,
        Played: el('browsePlayed').value, Favorites: el('browseFavorites').checked, Sort: el('browseSort').value, Descending: el('browseOrder').value === 'desc' };
    }
    var active = 0, waiting = [];
    function pump() {
      while (active < 3 && waiting.length) {
        var job = waiting.shift();
        if (job.signal.aborted) { job.reject(new Error('Cancelled')); continue; }
        active++;
        (function (task) { task.run().then(task.resolve, task.reject).finally(function () { active--; pump(); }); })(job);
      }
    }
    function limited(run, signal) { return new Promise(function (resolve, reject) { waiting.push({run: run, signal: signal, resolve: resolve, reject: reject}); pump(); }); }
    function link(id, title) {
      var a = node('a', title); a.href = context.webRoot + '/web/index.html#!/item?id=' + encodeURIComponent(id) + '&serverId=' + encodeURIComponent(catalog.ServerId); return a;
    }
    function itemTitle(item) {
      if (item.Kind === 'Season') return item.Number === 0 ? t('specials') : item.Number != null ? t('season') + ' ' + item.Number : item.Title || t('unknown');
      return item.Kind === 'Episode' && item.Number != null ? String(item.Number).padStart(2, '0') + ' — ' + item.Title : item.Title;
    }
    function row(item) {
      var li = node('li', '', 'browse-item');
      var parts = [t(item.Kind)]; if (item.Year) parts.push(String(item.Year)); if (item.Minutes) parts.push(item.Minutes + ' min'); if (item.Rating != null) parts.push('★ ' + Number(item.Rating).toLocaleString(context.language(), {maximumFractionDigits: 1}));
      li.appendChild(link(item.Id, itemTitle(item))); li.appendChild(node('span', parts.join(' · '), 'muted'));
      explainMatch(li, item); return li;
    }
    function explainMatch(parent, item) {
      if (item.MatchField && item.MatchValue) parent.appendChild(node('span', t(item.MatchField) + ': ' + item.MatchValue, 'muted match-reason'));
    }
    function groupTitle(group) {
      if (group.Kind === 'unknown' || group.Kind === 'other') return t(group.Kind);
      var n = Number(group.Label);
      if (group.Kind === 'month') return new Intl.DateTimeFormat(context.language(), {month: 'long', timeZone: 'UTC'}).format(new Date(Date.UTC(2000, n - 1, 1)));
      if (group.Kind === 'decade') return group.Label + '–' + (n + 9);
      if (group.Kind === 'rating') return group.Label + '–' + (n === 9 ? 10 : n + 1) + ' ★';
      return group.Label;
    }
    function branch(title, params, current, signal, level, itemId, ready) {
      var details = node('details', '', level === 'root' ? 'browse-group' : 'tree-branch'), summary = node('summary'), countLabel = node('span', '', 'muted');
      details.dataset.level = level; summary.appendChild(node('strong', title)); summary.appendChild(countLabel);
      if (itemId) { var a = link(itemId, t('openItem')); a.className = 'tree-link'; summary.appendChild(a); }
      details.appendChild(summary);
      var list = node('ul', '', 'browse-items'), message = node('p', '', 'muted'), button = node('button', t('more')); button.type = 'button'; button.hidden = true;
      details.appendChild(list); details.appendChild(message); details.appendChild(button);
      var group = {details: details, list: list, count: 0, countLabel: countLabel, message: message, button: button, loaded: !!ready, busy: false, complete: false, nodes: new Map()};
      details.addEventListener('toggle', function () { if (details.open && !group.loaded && !group.busy) page(group, 0, current, params, signal); });
      return group;
    }
    function attach(parent, child) { var li = node('li', '', 'tree-node'); li.appendChild(child.details); parent.list.appendChild(li); }
    function showSeason(view) {
      var show = el('browseShowEpisodes').checked;
      view.leaf.hidden = show; view.details.hidden = !show;
      if (!show) view.details.open = false;
    }
    el('browseShowEpisodes').addEventListener('change', function () {
      var searchingEpisodes = el('browseEpisodeMetadata').checked || el('browseKind').value === 'Episode';
      if (!this.checked) {
        includeEpisodeMetadata = false;
        if (el('browseKind').value === 'Episode') el('browseKind').value = '';
        syncEpisodeMetadata();
      }
      seasonViews.forEach(showSeason);
      if (!this.checked && searchingEpisodes) search();
    });
    function appendItem(group, item, params, current, signal) {
      var key = item.Kind === 'Episode' && params.Mode !== 'children' && item.SeriesId !== '0' ? item.SeriesId : item.Id;
      if (item.Kind === 'BoxSet') {
        if (group.nodes.has(key)) return;
        var box = branch(item.Title, Object.assign({}, params, {Mode: params.Sort === 'SortName' ? 'contents' : 'groups', BoxSetId: item.Id, ParentId: '', Group: ''}), current, signal, 'collection', item.Id);
        group.nodes.set(key, box); attach(group, box); return;
      }
      if (item.Kind === 'Season') {
        if (group.nodes.has(key)) return;
        var seasonBranch = branch(itemTitle(item), Object.assign({}, params, {Mode: 'children', ParentId: item.Id, Group: ''}), current, signal, 'season', item.Id);
        var container = node('li', '', 'tree-node'), leaf = node('div', '', 'browse-item');
        leaf.appendChild(link(item.Id, itemTitle(item)));
        container.appendChild(leaf); container.appendChild(seasonBranch.details); group.list.appendChild(container);
        group.nodes.set(key, seasonBranch);
        var view = {leaf: leaf, details: seasonBranch.details}; seasonViews.push(view); showSeason(view);
        return;
      }
      if (item.Kind === 'Series' || item.Kind === 'Season') {
        if (group.nodes.has(key)) return;
        var child = branch(itemTitle(item), Object.assign({}, params, {Mode: 'children', ParentId: item.Id, Group: ''}), current, signal, item.Kind.toLowerCase(), item.Id);
        explainMatch(child.details.querySelector('summary'), item);
        group.nodes.set(key, child); attach(group, child); return;
      }
      if (item.Kind === 'Episode' && params.Mode !== 'children' && item.SeriesId && item.SeriesId !== '0') {
        var series = group.nodes.get(key);
        if (!series) { series = branch(item.SeriesTitle, params, current, signal, 'series', item.SeriesId, true); series.matches = true; group.nodes.set(key, series); attach(group, series); }
        // A full series result already exposes all seasons; don't duplicate it for an episode match.
        if (!series.matches) return;
        var seasonKey = item.SeasonId || 'unknown', season = series.nodes.get(seasonKey);
        if (!season) {
          var name = item.SeasonNumber === 0 ? t('specials') : item.SeasonNumber != null ? t('season') + ' ' + item.SeasonNumber : t('unknown');
          season = branch(name, params, current, signal, 'season', seasonKey === '0' || seasonKey === 'unknown' ? '' : seasonKey, true);
          series.nodes.set(seasonKey, season); attach(series, season);
        }
        season.list.appendChild(row(item)); return;
      }
      group.list.appendChild(row(item));
    }
    async function page(group, start, current, params, signal) {
      if (signal.aborted || group.busy) return;
      group.busy = true; group.complete = false; group.button.disabled = true; group.message.textContent = t('loading');
      try {
        var result;
        do {
          var query = new URLSearchParams(Object.assign({}, params, {Start: start}));
          result = await limited(function () { return api('LibraryHub/Community/Browse?' + query.toString(), undefined, false, signal); }, signal);
          if (current !== sequence || signal.aborted) return;
          start = result.Next;
        } while (!(result.Items || []).length && !(result.Groups || []).length && result.Next != null);
        (result.Groups || []).forEach(function (g) {
          var child = branch(groupTitle(g), Object.assign({}, params, {Mode: g.HasGroups ? 'groups' : 'items', Group: g.Key}), current, signal, 'group'); attach(group, child);
          // Reveal a single matching path, while keeping large result trees lazy.
          child.details.open = hasFilters(params) && result.Groups.length === 1;
        });
        (result.Items || []).forEach(function (item) { appendItem(group, item, params, current, signal); });
        group.count += (result.Items || []).length + (result.Groups || []).length;
        group.countLabel.textContent = group.count + ' ' + t(params.Mode === 'groups' ? 'groupsShown' : group.count === 1 ? 'shownOne' : 'shown');
        group.message.textContent = group.count ? '' : t('empty'); group.loaded = true;
        if (searchMode && group.details.dataset.level === 'root') group.details.hidden = group.count === 0;
        group.button.hidden = result.Next == null; group.button.textContent = t('more'); group.button.disabled = false;
        group.button.onclick = function () { page(group, result.Next, current, params, signal); };
      } catch (error) {
        if (current !== sequence || signal.aborted) return;
        if (error.status === 401 || error.status === 403) { context.fail(error); return; }
        group.details.hidden = false;
        group.message.textContent = t('unavailable'); group.button.hidden = false; group.button.disabled = false; group.button.textContent = t('retry');
        group.button.onclick = function () { page(group, start, current, params, signal); };
      } finally {
        group.busy = false; group.complete = true;
        if (searchMode && current === sequence && !signal.aborted) updateSearchStatus();
      }
    }
    function updateSearchStatus() {
      if (!rootGroups.length) return;
      var pending = rootGroups.some(function (group) { return !group.complete; });
      var visible = rootGroups.some(function (group) { return !group.details.hidden; });
      el('browseStatus').textContent = pending ? t('searching') : visible ? '' : t('empty');
      el('catalogNavigation').hidden = !visible;
    }
    function hasFilters(values) { return !!(values.Search || values.Kind || values.Genre || values.Year || values.Played || values.Favorites); }
    async function search() {
      clearTimeout(timer); if (controller) controller.abort(); controller = new AbortController();
      var signal = controller.signal, current = ++sequence, values = filters(); el('browseResults').textContent = '';
      rootGroups = []; seasonViews = []; el('catalogNavigation').hidden = searchMode;
      if (!values) return;
      var roots = catalog.Libraries.filter(function (c) { return chosen.has(c.Id); });
      if (!roots.length) { el('browseStatus').textContent = t(catalog.Libraries.length ? 'choose' : 'noLibraries'); return; }
      if (searchMode && !hasFilters(values)) { el('browseStatus').textContent = t('searchPrompt'); return; }
      el('browseStatus').textContent = searchMode ? t('searching') : '';
      roots.forEach(function (c) {
        var params = Object.assign({}, values, {LibraryId: c.Id, Mode: c.IsCollections ? 'collections' : 'groups', Group: '', HideEmptyCollections: searchMode});
        var root = branch(c.Name, params, current, signal, 'root');
        rootGroups.push(root);
        if (c.IsCollections && !searchMode) root.details.insertBefore(node('p', t('collectionHint'), 'muted'), root.list);
        root.details.hidden = false;
        el('browseResults').appendChild(root.details);
        root.details.open = true;
        if (searchMode) page(root, 0, current, params, signal);
      });
    }
    function clear() { seasonViews = []; sequence++; clearTimeout(timer); if (controller) controller.abort(); el('browseResults').textContent = ''; el('libraryChoices').textContent = ''; el('browsePanel').hidden = true; }
    async function load() {
      var current = ++sequence;
      var result = await api('LibraryHub/Community/Libraries');
      if (current !== sequence) return;
      catalog = result;
      translate(); syncEpisodeMetadata();
      var available = catalog.Libraries;
      var saved; try { saved = JSON.parse(context.read('browseLibraries')); } catch (_) {}
      chosen = new Set(Array.isArray(saved) ? saved.filter(function (id) { return available.some(function (c) { return c.Id === id; }); }) : available.map(function (c) { return c.Id; }));
      renderChoices(); el('browsePanel').hidden = false; await search();
    }
    el('browseForm').addEventListener('submit', function (event) { event.preventDefault(); search(); });
    ['browseSearch', 'browseGenre', 'browseYear'].forEach(function (id) { el(id).addEventListener('input', function () { clearTimeout(timer); timer = setTimeout(search, 350); }); });
    function syncEpisodeMetadata() {
      var kind = el('browseKind').value;
      el('browseEpisodeMetadata').checked = kind === 'Episode' || (!kind && includeEpisodeMetadata);
      el('browseEpisodeMetadata').disabled = !!kind;
      if (el('browseEpisodeMetadata').checked) el('browseShowEpisodes').checked = true;
    }
    el('browseEpisodeMetadata').addEventListener('change', function () { includeEpisodeMetadata = this.checked; syncEpisodeMetadata(); search(); });
    el('browseKind').addEventListener('change', function () { syncEpisodeMetadata(); search(); });
    ['browseSearchField', 'browsePlayed', 'browseFavorites', 'browseSort', 'browseOrder'].forEach(function (id) { el(id).addEventListener('change', search); });
    el('browseSelectAll').addEventListener('click', function () { chosen = new Set(catalog.Libraries.map(function (c) { return c.Id; })); renderChoices(); search(); });
    el('browseSelectNone').addEventListener('click', function () { chosen.clear(); renderChoices(); search(); });
    el('browseReset').addEventListener('click', function () {
      var sort = el('browseSort').value, order = el('browseOrder').value;
      el('browseForm').reset(); renderChoices(); el('browseSort').value = sort; el('browseOrder').value = order;
      includeEpisodeMetadata = false; syncEpisodeMetadata(); search();
    });
    ['Expand', 'Collapse'].forEach(function (action) { el('browse' + action).addEventListener('click', function () { Array.from(el('browseResults').querySelectorAll('details')).filter(function (n) { return !n.hidden && (action === 'Collapse' || !n.parentElement.closest('details:not([open])'));  }).forEach(function (n) { n.open = action === 'Expand'; }); }); });
    return {load: load, clear: clear};
  };
})();

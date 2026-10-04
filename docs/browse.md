# Browse libraries

Open **Browse** in the standalone navigation, **Browse / Library Hub** in Emby Web’s user menu, or the link in plugin settings.

Permanent address: `https://media.example.com/emby/LibraryHub/Browse`.

The page uses the same remembered login as the archive and subscriptions. Each reader sees only media their Emby account can access.


These addresses are examples. Copy the links from plugin settings to preserve your server’s actual base path.

## Browse or search

**Browse** opens the full catalog: choose libraries, a sort criterion, then expand groups and titles.

**Search** has its own page at `https://media.example.com/emby/LibraryHub/Search`.

Choose **Search in** (default: **Title**) beside the text field, or use the other visible criteria.

Only libraries with matching items appear in search results. An empty search invites you to enter text or choose a criterion.

If nothing matches, one message is shown instead of a list of empty libraries.

Both pages share the remembered library selection, account permissions, and login.

The selection is remembered on this browser. Initially, all libraries available to your account are selected.
**Select all** and **Clear selection** apply to the whole accessible list.

Browse has one always-open control card: libraries, sorting, and **Show episodes**.

**Show episodes** is unchecked by default. Series expand into seasons; seasons remain links to Emby.

Check it to expand seasons into episodes. Switching it off hides episodes without closing the series.

Search has one fully open card: libraries, metadata search, other criteria, and sorting.

**Show episodes** sits beside sorting on both pages and is unchecked by default.
Matching series folders are combined using Emby’s series identity. Seasons and episodes follow their metadata, including nested episode folders and virtual seasons.
Season 0 is labelled **Specials**. Each series and season also has an **Open in Emby** link.
Title searches exclude episodes by default: searching for **Silo** returns the series, ready to expand.
Check **Also search episode metadata** to include episode matches beneath their series and season.

This also enables **Show episodes**. Turning **Show episodes** off disables episode metadata searching.

If the media type is **Episode**, turning **Show episodes** off returns the media type to **All**.
Choosing **Episode** as the media type automatically enables episode search; other specific media types disable it.
**Clear** turns this option off again.
An item belonging to several selected libraries appears under each of those roots.

Browse opens the full catalog immediately, without a search. Library roots open automatically.

Groups and titles load lazily as you expand them, with up to 50 entries per page.
**Load more** continues within that branch.
Counts indicate entries displayed so far, not the entire library. Hidden items are excluded.

Search text requires at least two characters. Other criteria can be used without text.

## Search metadata

Libraries come first. **Search in** and the text field share one row, stacking on narrow screens.

Choose Title, All metadata, Synopsis, Actors, Directors, Genres, Tags, Studios, or Year.

Title includes the displayed and original titles. All metadata combines the listed fields, including guest actors.

Matching ignores case and accents and looks for the entered phrase within the selected metadata.

For example, choose Actors and enter `Gary Oldman`, then narrow the results to unwatched movies.

Non-title matches show their source, such as an actor name or a short synopsis excerpt.

Text and other criteria update the same results automatically. **Clear** keeps libraries and sorting.

Episode metadata is searched only when enabled; the chosen field applies to episodes too.

## The Collections library

**Collections** appears once in the library picker. Individual box sets are nested inside that library.
Expand **Collections → Alien Collection → Alien**, for example. Box sets never become separate library choices.
The box-set list loads only when its library opens, alphabetically and in pages of 50.
On Search, only collections containing matching titles appear. Empty collection libraries are hidden.
With title sorting, opening a box set shows its titles directly. Other sorts use the usual groups inside it.
Series continue into seasons and episodes. Each title still respects the signed-in member’s Emby permissions.

## Sort

Browse sorting sits inside the control card. Search sorting sits below its filters.

Expand and collapse buttons below the card control only the catalog tree.

The selected sort determines the grouping inside each library:

| Sort         | Groups                                                                   |
| ------------ | ------------------------------------------------------------------------ |
| Title        | A–Z, 0–9, Other                                                          |
| Date added   | Year → month                                                             |
| Release date | Year → month                                                             |
| Year         | Decade → year                                                            |
| Rating       | Whole-point bands: 8 ≤ rating < 9, for example; the top band includes 10 |
| Director     | Letter → director → titles                                               |
| Actor        | Letter → actor → titles                                                  |

People are grouped by their full Emby display name, with accents folded for the initial letter.
Actor includes guest-star credits. A title appears under every credited person, once per person.
Names come only from matching titles your account can access; missing credits appear under **Unknown**.
Expand a person to browse their matching titles, with the same filters and paging as other criteria.

Title grouping uses Emby’s sort title, including its handling of articles and accents.
Added dates use the plugin’s configured timezone. Missing metadata appears under **Unknown**, always last.
**Other** also stays last for title sorting. Duration is not a sort option.

Choose ascending or descending order. Library roots are listed alphabetically.

**Clear** clears the search and filters, preserving library selection, sort criterion, and order.
**Expand one level** opens the next visible tree level. **Collapse all** closes every loaded branch.

## Library access

Administrators choose the libraries offered in **Catalog browsing** in plugin settings.

Members can select a subset of those libraries, limited by their existing Emby permissions.

The selection affects browsing only. Archives, emails, and access elsewhere in Emby remain unchanged.

## Server activity

Browsing reads the current Emby database. It does not scan media files, generate archives, or send email.
Group headers are calculated on demand from matching metadata, read in batches. Large libraries can take longer to group.
Items, seasons and episodes are fetched when their branches open; no persistent browse index is created.
Search input is debounced, obsolete requests are cancelled in the browser, and at most three browse requests run concurrently.

No API key appears in media links. Login and data requests use the existing authenticated community session.

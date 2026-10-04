# Development

## Prerequisites

- .NET 8 SDK for the plugin and core tests.
- Node.js 18 or newer for the settings-controller tests.
- Python 3 for packaging; Python 3.12 is used for documentation builds.

```sh
./tools/check
./tools/package
```

NuGet dependencies are locked in `packages.lock.json`. Restore uses locked mode.

`tools/check` runs compilation, core tests, UI controller tests, and an isolated plugin-loading smoke test.

SMTP tests use local test servers. They do not contact a production SMTP provider or send real user emails.

## Structure

| Directory                         | Responsibility                                           |
| --------------------------------- | -------------------------------------------------------- |
| `src/Emby.LibraryHub.Core/`       | Tracking, formatting, storage, subscriptions, SMTP, HTML |
| `src/Emby.LibraryHub/`            | Emby adapters, routes, scheduled task, settings UI       |
| `tests/Emby.LibraryHub.Tests/`    | Calendar, persistence, delivery, HTML, and SMTP behavior |
| `tests/ui/`                       | Browser-controller behavior with a mocked Emby API       |
| `tests/PluginLoadSmoke/`          | Isolated dependency loading and Emby HTTP contracts      |
| `docs/`                           | Website content and installation guide                   |
| `.github/workflows/`              | Prepared CI, Pages, and release workflows                |

Core source files are linked into the plugin assembly. Only one DLL needs to be installed.

## Use server-matched references

The default SDK package matches the target Emby version.

For additional compatibility checks, provide a directory containing the target server's API assemblies:

```sh
dotnet build src/Emby.LibraryHub -c Release -p:EmbyReferencePath=/path/to/server-api
dotnet run --project tests/PluginLoadSmoke -c Release -- \
  src/Emby.LibraryHub/bin/Release/net8.0/Emby.LibraryHub.dll /path/to/server-api
```

Do not commit or redistribute copied server assemblies. They are verification inputs, not plugin dependencies to deploy.

## Documentation

```sh
python3 -m venv .venv-docs
.venv-docs/bin/pip install -r docs/requirements.txt
.venv-docs/bin/mkdocs build --strict
python3 tools/check-docs.py
.venv-docs/bin/mkdocs serve
```

English pages use `.md`; French pages use `.fr.md`, with matching paths and internal links.

English is served at the root and French under `/fr/`. The language switch keeps the current page.

Navigation and search are localized. The theme uses system fonts, local assets, and no analytics.

See the [official i18n documentation](https://ultrabug.github.io/mkdocs-static-i18n/) for configuration details.

## Settings translations

`Configuration/locale.js` contains English and French messages. Pages mark translatable text with `data-digest-*`.

Controllers use Emby's active `globalize` locale and assign plain text, never translated HTML.

Tests cover user preferences, regional variants, fallback, and independence from report language.

## Make a change

1. Reproduce the behavior with focused tests or a documented runtime case.
2. Keep private member views, subscriber delivery, and administrative controls separate.
3. Run `tools/check`, the strict documentation build, and `tools/check-docs.py`.
4. Package the DLL and verify it on a test server before a public release.

Use synthetic addresses and media in fixtures. Never commit a live server configuration or subscriber store.

[Architecture](architecture.md) · [Release process](releases.md) · [Security](security.md)

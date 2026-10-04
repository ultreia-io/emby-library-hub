# Emby Library Hub

[![CI](https://github.com/ultreia-io/emby-library-hub/actions/workflows/ci.yml/badge.svg)](https://github.com/ultreia-io/emby-library-hub/actions/workflows/ci.yml)
[![Website](https://github.com/ultreia-io/emby-library-hub/actions/workflows/website.yml/badge.svg)](https://github.com/ultreia-io/emby-library-hub/actions/workflows/website.yml)
[![Release](https://img.shields.io/github/v/release/ultreia-io/emby-library-hub)](https://github.com/ultreia-io/emby-library-hub/releases)
[![License: GPL v3](https://img.shields.io/badge/license-GPL--3.0-blue.svg)](LICENSE)
![.NET 8](https://img.shields.io/badge/.NET-8-512BD4)
![Emby 4.11.0.5](https://img.shields.io/badge/tested%20API-Emby%204.11.0.5-52B54B)

**Explore your libraries, stay up to date.**

Browse your Emby libraries, read daily HTML archives, and receive private updates in French or English.

![Library Hub](docs/assets/banner.svg)

## What it does

- French/English settings following Emby’s display language, plus a fully bilingual documentation site.
- Browses selected libraries in a searchable tree, with filters and Emby sorting.
- Tracks additions and removals, grouped by library and calendar day.
- Publishes a private archive with clickable titles and series/season links.
- Offers account-bound signup with email confirmation, language choice, and unsubscribe.
- Sends at most one report per subscriber per day; empty days are skipped.
- Lets administrators view subscribers, remove addresses, preview reports, and export settings.
- Regenerates or resets HTML reports without sending email or changing subscriber delivery state.

There is no separate administrator recipient or manual historical email broadcast.

## Start here

1. [Download and install](docs/INSTALL.md) the single DLL, then restart Emby.
2. Choose offered libraries, the public server URL, report language, and time zone.
3. Open Browse or the private archive and share the link with your Emby members.
4. Optionally configure SMTP and enable subscriptions. Subscribe yourself if you want reports too.

[Installation](docs/INSTALL.md) · [Configuration](docs/configuration.md) · [Subscriptions](docs/subscriptions.md)

[Browse libraries](docs/browse.md) · [Archive and reset](docs/archive.md) · [Troubleshooting](docs/troubleshooting.md) · [Quick start](docs/quick-start.md)

## Compatibility

The build targets **Emby Server 4.11.0.5 on .NET 8** and uses its matching SDK.

Development testing includes Synology and isolated checks against that server's API assemblies.

Other server versions and platforms need validation before they can be advertised as supported.

Android TV users can subscribe in a browser on their phone or computer. An active Emby login is required.

All signed-in members share the same archive, including every reported library.

Browsing and emails respect each member’s Emby permissions. Playback uses the same Emby account.

## Build

Use the .NET 8 SDK, Node.js 18 or newer, and Python 3.

```sh
./tools/check
./tools/package
```

The package contains one plugin DLL, installation instructions, license, and dependency notices.

MailKit and MimeKit are embedded. Emby SDK assemblies must not be deployed with the plugin.

[Developer guide](docs/development.md) · [Release process](docs/releases.md) · [Architecture](docs/architecture.md)

## Documentation website

```sh
python3 -m venv .venv-docs
.venv-docs/bin/pip install -r docs/requirements.txt
.venv-docs/bin/mkdocs serve
```

The GitHub Pages address is `https://ultreia-io.github.io/emby-library-hub/`.

Local builds do not publish anything. GitHub Actions builds the website and prepares tagged releases.

## Status and license

**0.1.0 is the first release line.** See GitHub Releases for available downloads.

The plugin is not yet in Emby's official catalog and is not endorsed by Emby.

[Catalog submission preparation](docs/emby-catalog.md) · [Changelog](CHANGELOG.md) · [Security](SECURITY.md)

Copyright © 2026 Tony Chemit. Licensed under **GPL-3.0-only**; see [LICENSE](LICENSE).

Embedded dependencies retain their MIT licenses in [third-party notices](docs/THIRD-PARTY-NOTICES.txt).

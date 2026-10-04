# Contributing

Use the current `develop` branch unless the maintainer requests a separate branch.

Read the [developer guide](docs/development.md) and [architecture](docs/architecture.md) before changing behavior.

Run `./tools/check`, `./tools/package`, and a strict MkDocs build for relevant changes.

Add focused tests for calendar boundaries, persisted delivery state, and authenticated endpoint behavior.

Keep HTML generation independent from subscriber delivery. Never reintroduce a manual mailing-list broadcast implicitly.

Use synthetic media and email addresses in tests, screenshots, and issue reports.

Do not commit server credentials, subscriber stores, private URLs, copied Emby assemblies, or build artifacts.

Contributions are provided under the project's GPL-3.0-only license.

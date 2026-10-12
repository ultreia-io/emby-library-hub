# Changelog

## 0.1.1 — 2026-10-12

Portable runtime target and documentation update. Stored data formats are unchanged.

- Fix empty async API responses affecting report generation, archive viewing, preview, reset, and subscriber administration.
- Display the release version in every plugin and documentation page footer.
- Link to the GitHub repository from every plugin page footer.
- Include the release version in the downloadable DLL, ZIP, and checksum filenames.
- Target .NET Standard 2.0 for the plugin and core library.
- Replace newer runtime APIs with portable collection, cryptography, and dependency loading code.
- Embed the mail dependency closure and its license notices in the single plugin DLL.
- Separate administration and user guides in English and French, with navigation by audience.
- Three administration diagrams covering access rules, daily delivery, and maintenance.
- Eight screenshots per language, using the plugin UI and fictional demonstration data.
- Dedicated archive administration instructions, separate from the member archive guide.
- Clearer account, shared-archive, email-delivery, and browser-access guidance.
- Reproducible visual-generation tools and improved dark-theme button contrast.
- Remove the maintainer's Emby submission preparation from the documentation.

## 0.1.0 — 2026-10-04

First release of Library Hub.

- French/English settings following Emby’s display language, plus a fully bilingual documentation site.
- Private daily HTML archive, date-range generation, and reset with backup.
- Confirmed mailing-list subscriptions with French/English choice and unsubscribe.
- Subscriber administration and at most one daily report per subscriber.
- Collections is one library choice, with lazily loaded box sets and titles inside it.
- Standalone multi-library search with filters, sort-based groups including directors and actors, and lazy series/season/episode browsing.
- Separate Browse and Search pages: full catalog browsing, visible search filters, and no empty search libraries.
- Administrators choose offered libraries; members select a subset within their Emby permissions.
- Fully open control card per page; optional episode expansion on both pages, linked to episode metadata search.
- Metadata search selector, including people, synopsis, tags, studios, and year, with match explanations.
- Copyright and ultreia.io branding on HTML pages.
- Single-DLL deployment, TLS SMTP, and configuration import/export.
- GPL v3 licensing, documentation website, CI, and draft-release workflows.

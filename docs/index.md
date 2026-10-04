# Explore your libraries, stay up to date

![Library Hub](assets/banner.svg)

**Library Hub** brings library browsing, private daily archives, and subscriber emails to your Emby server.

[Install the plugin](INSTALL.md){ .md-button .md-button--primary }
[Quick start](quick-start.md){ .md-button }

## Find something to watch

Search across selected libraries, combine filters, and browse by title, date, rating, director, or actor.
Expand series into seasons and episodes. [Explore browsing](browse.md).

## A daily archive

Browse additions and removals by date. Open movie links or series pages directly from each report.

Series episodes are grouped by title and season. The archive index groups report days by month.

## A mailing list people control

Readers choose French or English, confirm their email, and unsubscribe whenever they want.

Each receives a separate message. Administrators see membership and delivery status without exposing addresses publicly.

## Rebuild without resending

Generate older HTML reports or reset the archive index. Neither action changes subscriber delivery state.

Daily emails have their own persisted delivery records and a one-report-per-calendar-day limit.

## Before you begin

- Target server: **Emby 4.11.0.5**, running .NET 8.
- Optional email: SMTP with TLS on port 465, or STARTTLS on another port such as 587.
- A public server URL for subscriptions and report links.
- Manual installation until an Emby catalog listing is accepted.

Shared links require Emby sign-in. All members share the saved archive, including every reported library.

Browsing and emails respect the member’s Emby permissions.

[Configure delivery](configuration.md) · [Manage subscribers](subscriptions.md) · [Manage the archive](archive.md)

# Explore your libraries, stay up to date

![Library Hub](assets/banner.svg)

**Library Hub** brings library browsing, private daily archives, and subscriber emails to your Emby server.

<div class="guide-paths" markdown>

<div class="guide-path" markdown>

### I use Library Hub

Sign in, browse, search, read daily updates, and manage your email subscription.

[Open the user guide](user-guide.md){ .md-button .md-button--primary }

</div>

<div class="guide-path" markdown>

### I administer the server

Install the plugin, choose access, configure delivery, and protect your data.

[Open the administration guide](administration.md){ .md-button }

</div>

</div>

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

- API baseline: **Emby 4.11.0.5**. Plugin target: **.NET Standard 2.0**.
- Optional email: SMTP with TLS on port 465, or STARTTLS on another port such as 587.
- A public server URL for subscriptions and report links.
- Manual installation using the DLL from a published release.

Shared links require Emby sign-in. All members share the saved archive, including every reported library.

Browsing and emails respect the member’s Emby permissions.

[Configure delivery](configuration.md) · [Manage subscribers](administration.md#4-enable-subscriber-emails-if-wanted) · [Manage the archive](archive-administration.md)

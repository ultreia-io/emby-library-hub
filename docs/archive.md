# Daily HTML archive

The archive link opens the HTML report list directly. Use **My subscription** to manage email delivery.

The archive groups published report days by month. Each day lists additions and removals by library.

Today remains marked as in progress until the next calendar day.

## Standalone permalinks

- Archive: `https://media.example.com/emby/LibraryHub/Archive`
- Daily report: `https://media.example.com/emby/LibraryHub/Archive/2026-10-03`
- Subscription management: `https://media.example.com/emby/LibraryHub/Subscriptions`

These are standalone pages, outside Emby's dashboard. When needed, sign in on the page with your Emby username and password.

The current Emby Web login is reused when available on the same browser origin and server.
Otherwise, “Remember me on this browser” keeps you signed in across tabs and email links; uncheck it for a shared browser.
Another browser, device, or site address may require its own login.

Login opens the requested archive or date without changing its permalink. Android TV users can open the same links on a phone or computer.

The archive and subscription pages are separate. Saved HTML is read directly; viewing does not regenerate reports.


These addresses are examples. Copy the links from plugin settings to preserve your server’s actual base path.

## Open and share

Use **Open private archive** in settings or **Nouveautés / Library updates** in the Emby web user menu.

Native Android and TV clients may hide custom plugin pages. Open the same URL in a browser instead.

Every reader signs in to Emby. All members see the same saved reports, including all reported libraries.

The member page includes account-bound subscriptions. Watching videos requires the same Emby account.

## Generate older reports

Select From/To using **YYYY/MM/DD**, then click **Generate HTML only**.

Both dates are included. The end date cannot be after today in the configured time zone.

Generation updates HTML files only. It never sends emails or changes subscribers' delivery cursors.

## Reset and rebuild

1. Press **Reset archive list**.
2. Choose the dates you want to keep.
3. Press **Generate HTML only**.

A private backup is written before the private index is cleared.

Settings, subscribers, tracked changes, and the archive share identifier are preserved.

Automatic publication resumes with the reset calendar day. Older days return only when explicitly generated.

Old daily links return 404 until their day is regenerated. The archive and subscription links stay stable.

Member requests read the published HTML files directly after authentication. Viewing never regenerates reports.

## Where historical dates come from

Observed additions and removals use the plugin's recorded event dates.

For older additions, explicit HTML generation can use Emby's `DateCreated` metadata as a fallback.

Those values can reflect old file dates rather than the date you remember adding a video to this server.

Regenerating the same range uses the same source dates. Reset does not alter Emby's media metadata.

A series name may appear on several days because different episodes have different recorded dates.

Episodes are grouped by series and season. Unknown season numbers are omitted, never invented.

Removals before tracking began cannot be reconstructed. Subscriber mail does not use historical catalog backfill.

In subscriber emails, deleted titles are omitted when fine-grained permissions cannot be verified. See [privacy and security](security.md).

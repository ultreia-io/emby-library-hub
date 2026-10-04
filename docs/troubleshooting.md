# Troubleshooting

## The plugin or settings page is missing

Confirm that only `Emby.LibraryHub.dll` was copied into the plugin directory, then restart Emby.

Check **Dashboard → Plugins** and **Advanced → Library Hub**. Refresh the web app with Ctrl+Shift+R.

Only administrators can open settings. The private archive is a separate page.

## Settings remain in English

Choose French in Emby's display preferences, then reopen the plugin settings.

After updating the DLL, restart Emby and refresh its web interface with Ctrl+Shift+R.

The report language controls archive and preview content, not the interface language.

## Android TV has no plugin menu

Some native clients hide custom plugin pages. Use the archive or signup link on a phone or computer.

This does not affect daily subscriber delivery.

## Signup email does not arrive

Check the address, spam folder, sender authorization, SMTP credentials, and TLS mode.

You can request a new link immediately, including after unsubscribing. Use the newest email; earlier pending links are replaced.

If sending fails or the address belongs to another account, the page displays an error.
Ask the administrator to check the Emby log for delivery errors.

## Daily reports do not arrive

Check the following in order:

1. The address appears as **Subscribed**, not awaiting confirmation.
2. Community subscriptions and daily subscriber delivery are both enabled.
3. The saved delivery time has passed in the configured time zone.
4. There are completed days with observed changes since confirmation.
5. The subscriber has not already received a report today.
6. The scheduled task is enabled and the subscriber list shows no SMTP error.

Empty days never produce emails. Failed delivery retries after an hour.

A queued older report is delivered before newer ones; catch-up remains limited to one report per day.

## HTML generation shows surprising old dates

Compare the report with Emby's creation metadata. Historical backfill can reflect old filesystem dates.

A repeated series title may stand for different episodes. Missing season metadata produces a title-only group.

Reset the archive and regenerate a later range if you do not want those older entries published.

## A report link returns 404

Check that the full share URL was copied and that the requested day is published.

After an archive reset, an old daily link works again only after that day is regenerated.

Restore `archive.json` and its daily HTML files together from the same backup.

## An operation fails

Read the Emby server log around the failure time. On Synology, logs are usually under the package's `var/logs/`.

A library scan or server startup may postpone reconciliation. Retry after the scan finishes.

A missing or corrupt state file requires recovery, not deleting the data directory.

When reporting a bug, include plugin version, Emby version, OS, steps, and a redacted stack trace.

Remove authentication tokens, share URLs, SMTP credentials, subscriber addresses, and media paths before sharing logs.

In subscriber emails, deleted titles are omitted when fine-grained permissions cannot be verified. See [privacy and security](security.md).

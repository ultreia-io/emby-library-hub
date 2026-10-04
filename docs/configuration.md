# Configuration

Open **Dashboard → Advanced → Library Hub** in Emby's web interface.

Use **Save** after editing settings. A server restart is not needed.

## Catalog browsing

Choose the libraries offered to members. Each member sees only those their Emby permissions allow.

All libraries are offered initially. Saving settings records the checked libraries; uncheck all to offer none.

After saving a selection, newly created libraries must be checked here to become available.

Members can choose a smaller selection. With no filters, Browse opens their full selected catalog.

This setting is included in JSON export and restore. It affects browsing only, not archives or emails.

## Interface language

The settings page follows Emby's active display language, including the user's display preference.

French and its regional variants are supported. Other languages fall back to English.

Labels, help text, buttons, messages, and subscriber statuses are translated.

This does not change the archive language or each subscriber's chosen email language.

## Delivery and language

| Setting                     | Purpose                                                             |
| --------------------------- | ------------------------------------------------------------------- |
| Daily subscriber emails     | Enables scheduled reports to confirmed subscribers.                 |
| Community subscriptions     | Enables new signups and subscriber delivery.                        |
| Delivery time               | Earliest local time at which the scheduled task sends a report.     |
| Time zone                   | Calendar-day boundary, for example `Europe/Paris` or `Etc/UTC`.     |
| Report and preview language | French or English for generated HTML and the administrator preview. |

Subscribers choose their own email language independently of the archive language.

Both switches must be on for daily subscriber reports. Signup confirmations can be sent while daily delivery is off.

Turning subscriptions off pauses signup and subscriber reports. Existing unsubscribe links continue to work.

## Server URL

Enter the externally reachable base URL, such as `https://media.example.com`.

Do not append `/web/index.html`, credentials, a query string, or an item link.

Include a reverse-proxy base path only if it is part of the server's actual public address.

Open the archive link outside your home network before sharing it.

**Include links to added videos** controls playback links. Removed items do not receive playback links.

## SMTP

| Setting   | Example                                                     |
| --------- | ----------------------------------------------------------- |
| Server    | `smtp.example.com`                                          |
| Sender    | `library@example.com`                                       |
| Port      | `465` for implicit TLS; often `587` for STARTTLS            |
| Username  | The SMTP account identifier                                 |
| Password  | The account's SMTP password or provider-issued app password |

Port 465 always starts with TLS. For other ports, enable STARTTLS when supported by your provider.

Authenticated SMTP without TLS is rejected. Certificate validation stays enabled.

The sender address must be accepted by your SMTP provider. Subscriber addresses are individual destinations.

## Preview and date selection

**Preview today's report** refreshes tracking and displays content without emailing anyone.

The date fields display **YYYY/MM/DD**. The Calendar button opens the browser's date picker when supported.

Generate HTML accepts inclusive From/To dates and rejects dates later than today in the saved time zone.

## Backup and restore

**Export JSON** downloads the saved configuration, not unsaved edits.

The export contains the SMTP password. Keep it private and out of Git repositories and support tickets.

**Restore from JSON** validates and saves the settings immediately. Invalid files leave the configuration unchanged.

Subscriber data and tracked history are separate files; see [backup and recovery](backup.md).

Replacing the DLL preserves settings. Restore JSON only when you deliberately want to replace the saved configuration.

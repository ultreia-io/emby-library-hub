# Architecture

## Shared history, saved archive

Emby events and inventory reconciliation feed persistent history.

Explicit generation and scheduled publication maintain the HTML files and published-day index.

Archive viewing reads those saved files after Emby authentication, with no media queries or regeneration.

All signed-in members see the same archive. Email delivery still checks each recipient’s current permissions.

Archive and subscription management use separate pages and controllers.

## Browser catalog

Browse and Search use read-only Emby queries, scoped to the member and the administrator's offered libraries.
Metadata search reads batches and filters visible items before returning matches or group names.
Series, seasons, and episodes load on demand; there is no persistent catalog copy.

## Authentication

The standalone shell and fixed assets are anonymous; data services require authentication.
Management routes additionally require the administrator role.

Member endpoints resolve identity from `IAuthorizationContext` and reject missing or inactive user accounts.

Clients cannot supply a user ID. Generic server API keys without a user cannot read member reports.

The web page uses authenticated JSON requests and a sandboxed `srcdoc` frame, without credentials in URLs.

Files and backups remain private server data; the anonymous shell contains neither reports nor member details.

## Account-bound subscriptions

Each record has an Emby owner and a pending owner for new confirmation requests.

Signed form proofs are bound to the account and action. Email confirmation tokens are hashed and expire after 24 hours.

Only the requesting account can confirm. Another account cannot transfer a confirmed address or remove its record.

Only confirmed records with an eligible Emby account can receive reports.

## Calendar delivery and permissions

Days run from local midnight to the next midnight in the configured time zone.

The five-minute task waits until the configured delivery time and handles at most 100 queue steps per invocation.

Each address persists its next day, pending message ID, retry time, and last successful send.

Every attempt rebuilds the body using current permissions, preserving the message ID when retrying the same day.

Saved plaintext never bypasses permission checks. A completely hidden report is skipped without sending mail.

Successful delivery limits that subscriber to one report per local calendar day; missed days catch up oldest first.

## Storage and concurrency

The coordinator serializes inventory and history mutations. A separate gate protects subscription changes and delivery.

Messages are saved before SMTP. Failures defer the address for an hour; other recipients continue independently.

Files are replaced atomically with backup copies. Missing primary state does not silently create a new history.

SMTP acceptance and local persistence cannot commit atomically, so uncertain failures can produce duplicates.

Archive generation and reset do not invoke SMTP or change subscriber progress.

## Language and dependencies

Settings read Emby's active display locale independently of report and subscriber language choices.

MailKitLite and MimeKitLite are embedded. Only the plugin DLL is deployed.

Tests verify core isolation, account ownership, permission changes, browser behavior, and actual Emby API contracts.

# Community email subscriptions

Share the archive link from settings. **My subscription** opens signup; **Open private archive** returns to reports.

## Join with your Emby account

1. Open the community link or **Nouveautés / Library updates** in Emby's web user menu.
2. Sign in with your own Emby account.
3. Open **My subscription**, enter your email address and choose **Français** or **English**.
4. Request a confirmation email, then open its link within 24 hours.
5. Sign in with the same account and press **Confirm my subscription**.

Android TV users can do this in a phone or computer browser using their normal Emby account.

Opening a confirmation link alone changes nothing. This protects against automatic email scanners.

Another Emby account cannot confirm your request, take over your email address, or remove your subscription.

## Manage or unsubscribe

Your member page lists only your own subscriptions. Use **Unsubscribe** beside your address.

Each report also contains a sign-in link for unsubscribe; open it and confirm the action.

Unsubscribe remains available when subscriptions are paused. An administrator can remove an address for you.

To change language, request confirmation again for the same address while signed into the same account.

The old language remains active until confirmation. New language settings apply to newly prepared reports.

## For administrators

Share the archive URL from **Private community reports**. Readers must already have an Emby account.

**Refresh subscribers** shows address, language, confirmation state, and failures. **Unsubscribe** removes a record.

The administrator also subscribes through the member page; there is no separate administrator recipient.

## Daily delivery

- Reporting begins on the calendar day of account-linked confirmation.
- Only completed days with changes visible to that account produce emails.
- Each confirmed address receives at most one successfully recorded report per local calendar day.
- Backlogs catch up one report per delivery day, oldest first.
- Failed SMTP attempts retry after an hour without blocking other addresses.
- Account and library permissions are rechecked before every attempt, including queued retries.
- Disabled, deleted, locked-out, or currently schedule-blocked accounts receive no reports.
- Confirmation emails are separate from daily reports.

Archive generation, reset, and preview never trigger subscriber delivery or reset its progress.

SMTP acknowledgement and local storage are not atomic; an uncertain failure can still cause a retry duplicate.

## Confirmation requests

Each request sends a new confirmation link immediately, including after unsubscribing.
The latest link replaces earlier pending links for that address without creating a duplicate subscription.
An active subscription and its daily delivery progress remain intact until you unsubscribe.

The page displays validation, account ownership, and delivery errors instead of silently ignoring requests.
An address owned by another Emby account cannot be reassigned through signup.

Confirmations expire after 24 hours; stale unconfirmed records are cleaned during later requests.
Each scheduled invocation processes at most 100 delivery queue steps.

See [privacy and security](security.md) for deleted-item restrictions.

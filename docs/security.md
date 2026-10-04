# Privacy and security

## A closed Emby community

Reports, signup, confirmation, and self-service unsubscribe require an active Emby account.

A shared URL grants no access by itself. Saved reports and subscription actions require sign-in.

Report APIs take the user identity from Emby's authenticated session, never from a supplied user ID.

All active, signed-in Emby members read the same saved HTML reports, including every reported library.

Archive viewing reads existing files only. It never rebuilds reports, scans media, or sends email.

Administrator settings, generation, preview, reset, and the full subscriber list remain administrator-only.

## Email access

An address belongs to the Emby account that requested and confirmed it. Another account cannot take it over.

Delivery checks that the account exists, is enabled, is not locked out, and is within its permitted access schedule.

Permissions are checked again when preparing every send, including retries. Saved plaintext queues are never trusted.

Each message has one recipient. Account revocation blocks future reports; it cannot recall an email already received.

Email recipients can forward their messages. Subscribe only addresses you control and trust.

## Removed media in emails

Emby checks live media visibility. Deleted media no longer has a live item against which to check fine-grained rules.

Historical removed titles are shown only with current library access and no item-level restrictions.

Accounts with rating, tag, unrated-item, or excluded-subfolder restrictions do not see unverifiable deleted titles.

This deliberately omits some changes instead of exposing material whose access cannot be established.

## Browser and credentials

The standalone login page authenticates through Emby's own API. Passwords are sent only to that server and cleared from the form.

“Remember me on this browser” is enabled by default. It keeps the session token in local browser storage across tabs and visits.
Uncheck it to keep a new login in the current tab only. Passwords are never stored.
Tokens are sent in request headers, never URLs.

If no Library Hub session is saved, the page can reuse Emby Web's selected account on the same browser origin.
It first matches the server ID and never selects another saved user. Borrowed tokens are not copied into persistent storage.
Emby Web logout or account changes refresh pages using that borrowed session.

Sign out clears Library Hub sessions across tabs and asks Emby to revoke the token, including a reused Emby Web token.
Explicit logout prevents automatic Emby login reuse until the next manual Library Hub login.
Revoked sessions return to the login form. Browser storage restrictions may require signing in again.

Anonymous routes serve only the login shell and fixed JavaScript. Report HTML and membership data require authenticated API requests.

The page uses a restrictive Content Security Policy and clears displayed private data when leaving the page.

Saved HTML is displayed in a sandbox without scripts. Responses use `Cache-Control: no-store`.

Leaving the page clears displayed reports and addresses; late responses cannot restore the old report.

Media text is HTML-escaped. Settings exports include SMTP credentials and must remain private.

Authenticated SMTP requires TLS and certificate validation stays enabled.

Backups contain private collection and subscriber data. Restrict filesystem access to them.

## Vulnerability reports

Before publication, use the private maintainer review channel. Afterwards use GitHub's private vulnerability reporting.

Never place credentials, subscriber data, private URLs, or authentication headers in public issues.

The documentation website remains public and uses generic examples only.

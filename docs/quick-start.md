# Quick start

Start with browsing and archives; enable email only if you want a mailing list.

## Administrator

1. [Download and install](INSTALL.md) the DLL, restart Emby, then open **Dashboard → Advanced → Library Hub**.
2. Choose offered libraries, report language, time zone, and public server URL. Save.
3. Open **Browse libraries** to check the catalog. Use **Search** for titles or other metadata.
4. Open **Open private archive** and share its link with your Emby members.
5. Keep the **Library Hub** scheduled task enabled. It runs every five minutes by default.

All signed-in members share the same archive, including every reported library.
Browsing and emails respect each member's Emby permissions.

For a new archive, generate a small date range if you want historical additions.
Use **YYYY/MM/DD** dates and **Generate HTML only**; this sends no email.

## Optional mailing list

1. Configure [SMTP and delivery time](configuration.md), then enable community subscriptions and save.
2. Open **My subscription**, enter your address, choose a language, and request confirmation.
3. Follow the email link using the same Emby account and click **Confirm my subscription**.
4. Enable daily subscriber emails and save when ready.

Each confirmed address receives at most one successfully recorded daily report per calendar day; empty days are skipped.
Reports cover completed days in the configured time zone. Confirmation emails are separate.

## Members

Open the shared link in a browser and sign in with your usual Emby account.
The same-origin Emby Web login can be reused; otherwise the page offers **Remember me on this browser**.

Use **Browse** for the full selected catalog or **Search** for matching metadata and other criteria.
Open **Archive** for daily changes and **My subscription** to subscribe or unsubscribe.

Android TV users can open the same link on a phone or computer. Playback links open Emby.

## Before updating

Back up configuration and the complete `data/library-hub/` directory while Emby is stopped.
Replacing only the DLL preserves settings, subscribers, and reports.

[Configuration](configuration.md) · [Browsing](browse.md) · [Troubleshooting](troubleshooting.md) · [Backup](backup.md)

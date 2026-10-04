# Install Library Hub 0.1.0

Library Hub adds browser-based catalog browsing, daily HTML archives, and optional email subscriptions to Emby.

## Before you install

- Target: **Emby Server 4.11.0.5 on .NET 8**. Synology is the development installation; other versions need validation.
- You need administrator access to Emby and write access to its plugin directory.
- Choose a maintenance window: restarting Emby interrupts playback.
- SMTP is needed only for email subscriptions. Browsing and archives work without it.
- Readers need an active Emby account and a web browser, including users who normally watch through Android TV.

**Archive access:** all signed-in members see the same saved reports, including every reported library.
Browsing and email reports respect each member's Emby permissions.
The administrator's offered-library selection affects browsing only.

## 1. Download

Open the [GitHub releases page](https://github.com/ultreia-io/emby-library-hub/releases).
Download **emby-library-hub-0.1.0.zip** and **SHA256SUMS** from the 0.1.0 release assets.
Use the plugin ZIP, not GitHub's automatically generated source archives.

If no published release is available yet, build these assets locally using `./tools/package`.
A normal installation does not need a .NET SDK, Node.js, Python, or a source checkout.

On Linux, check the downloaded ZIP from the directory containing both files:

```sh
sha256sum --check --ignore-missing SHA256SUMS
```

The ZIP must be reported as **OK**. Extract it and locate **Emby.LibraryHub.dll**.
The DLL embeds its mail dependencies. Install only that DLL, not Emby SDK or separate mail assemblies.

## 2. Install on the server

Library Hub is not yet in Emby's official plugin catalog. The catalog does not provide a local DLL upload button.

### Synology

1. Stop **Emby Server** in DSM **Package Center**.
2. Copy `Emby.LibraryHub.dll` into `/var/packages/EmbyServer/var/plugins/`.
3. Ensure the Emby service account can read it; mode `644` is suitable when parent directories are accessible.
4. Start **Emby Server** in Package Center.
5. Refresh Emby Web with **Ctrl+Shift+R** and sign in as an administrator.
6. Open **Dashboard → Advanced → Library Hub**.

Use File Station or SSH with an account permitted to write to the package directory.
If copying reports **Permission denied**, correct those permissions; do not proceed with a partial replacement.

### Other installations

Locate the active server's Emby data directory, then its `plugins` directory.
Stop Emby, copy the DLL there with service-readable permissions, and start Emby again.
For containers, use the persistent data volume belonging to that container.
Do not copy the DLL into an Emby client application's directory.

## 3. Set up browsing and archives

In **Library Hub** settings:

1. Under **Catalog browsing**, check the libraries to offer to members.
2. Choose **Report and preview language** and the server's reporting **Time zone**.
3. Enter **Public Emby server URL**, for example `https://media.example.com`.
4. Leave both email switches off if you only want browsing and archives.
5. Click **Save**.

Use the server's base address, without `/web/index.html`, an item link, or an access token.
Include a reverse-proxy base path only when it is part of your server's actual address.

Settings apply immediately. Keep the **Library Hub** scheduled task enabled at its default five-minute interval.

Open **Browse libraries**, **Search libraries**, and **Open private archive** from settings.
Copy the **Archive link (Emby sign-in required)** to share the standalone site.
Members use its **Browse**, **Search**, **Archive**, and **My subscription** navigation.

## 4. Enable email subscriptions (optional)

1. Enter the sender address, SMTP server, port, username, and password supplied by your mail provider.
2. For port **465**, TLS starts automatically. For port **587**, enable **Use STARTTLS on ports other than 465**.
3. Set **Delivery time** and confirm the **Time zone**.
4. Check **Enable community subscriptions and subscriber delivery**, then save.
5. Open **My subscription**, enter your own email address, and request confirmation.
6. Follow the email link, sign in with the same Emby account if asked, and click **Confirm my subscription**.
7. Check **Enable daily subscriber emails**, then save when ready for daily delivery.

The administrator subscribes like any other member. Each person chooses French or English for their email.
The archive language and Emby's interface language are separate settings.

Only completed days with changes produce daily email. Tracking starts from the account's confirmation day.
Reports are sent after the configured time, with at most one successfully recorded report per address per day.
Generating HTML or previewing a report never sends email.

## 5. Verify the installation

- Browse opens the libraries selected by the administrator and permitted for the signed-in member.
- Search with at least two characters, or choose another criterion, and open a matching title in Emby.
- **Preview today's report** displays tracked changes, or reports that there are none.
- An empty archive on first installation is possible; it does not mean installation failed.
- To publish earlier additions, choose a small From/To range in **YYYY/MM/DD** and click **Generate HTML only**.
- If email is enabled, verify that your address appears as **Subscribed** after confirmation.

Historical additions use Emby's recorded dates. Removals before tracking began cannot be reconstructed.
Do not reset an archive just because it is empty; first check the selected dates and tracking history.

## Updating or removing the plugin

Before updating, stop Emby and back up the DLL, configuration, and complete `data/library-hub/` directory.
On Synology, these paths are relative to `/var/packages/EmbyServer/var/`:

| Path                                         | Contents                             |
| -------------------------------------------- | ------------------------------------ |
| `plugins/Emby.LibraryHub.dll`                | Installed plugin                     |
| `plugins/configurations/Emby.LibraryHub.xml` | Settings, including SMTP credentials |
| `data/library-hub/`                          | History, subscriptions, and archives |

Replace only the DLL, start Emby, and refresh the browser. Keep backup DLLs outside the live `plugins` directory.
Settings, subscribers, and archives are retained; JSON import is not required after each update.
If you need to roll back, stop Emby before restoring the previous DLL and its matching data backup.

To remove the plugin, stop Emby, remove its DLL, then restart Emby.
Keep configuration and data backups if you may reinstall. Removing the plugin stops its scheduled work.

## Help

[Configuration](https://ultreia-io.github.io/emby-library-hub/configuration/) ·
[Troubleshooting](https://ultreia-io.github.io/emby-library-hub/troubleshooting/) ·
[Backup and recovery](https://ultreia-io.github.io/emby-library-hub/backup/)

Never share configuration exports, SMTP passwords, subscriber files, or unredacted authentication logs.

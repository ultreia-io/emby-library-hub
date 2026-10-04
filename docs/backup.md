# Backup and recovery

Replacing the plugin DLL preserves configuration, tracked history, subscribers, and the archive.

Stop Emby before taking or restoring a consistent filesystem backup.

## Files to keep

Paths below are relative to Emby's data directory.

| Path                                            | Contents                                                 |
| ----------------------------------------------- | -------------------------------------------------------- |
| `plugins/configurations/Emby.LibraryHub.xml`    | Settings and SMTP credentials                            |
| `data/library-hub/state.json`                   | Inventory and observed change history                    |
| `data/library-hub/subscribers.json`             | Membership, tokens, delivery progress                    |
| `data/library-hub/reports/`                     | HTML pages, share identifier, index, and reset backups   |

Back up the complete directory set together. A configuration JSON export does not include subscriber or archive data.

All backups contain private information. Restrict their filesystem access and avoid public storage.

## Atomic state writes

State updates write a temporary file and replace the previous file, keeping a `.bak` copy.

If the main file is missing but its backup exists, the plugin fails rather than silently starting over.

Inspect the server log and restore the appropriate consistent snapshot while Emby is stopped.

Never delete `subscribers.json` to resolve a mail error. That also loses consent and delivery records.

Restoring an old subscriber snapshot can restore old membership and delivery progress. Check it before resuming email.

## Recover a reset archive

Reset snapshots live under `data/library-hub/reports/reset-backups/<timestamp-id>/`.

With Emby stopped, restore `archive.json`, `index.html`, and the daily HTML files from the same snapshot.

Restore the index and daily reports together so the list matches the available files.

Restart Emby and verify a known report through the private archive.

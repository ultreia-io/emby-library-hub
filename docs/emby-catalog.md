# Emby catalog submission preparation

Library Hub is not listed in the official catalog. This page prepares Tony's later submission.

Emby's guide says to request a developer identity from its team, then define a server plugin in its package portal.

The package needs descriptions, its identifier, target DLL, images, and version requirements.

Catalog versions must match the DLL assembly version. Review the current policy before submitting.

Source: [Emby catalog guide](https://betadev.emby.media/doc/plugins/dev/Getting-your-plug-in-in-the-catalog.html).

## Proposed listing

| Field                      | Prepared value                                      |
| -------------------------- | --------------------------------------------------- |
| Name                       | Library Hub                                         |
| Plugin identifier          | `f6975142-a690-4cdc-b98b-2c43b2d084ed`              |
| Target                     | Emby Server                                         |
| Target file                | `Emby.LibraryHub.dll`                               |
| Release version            | `0.1.0` — assembly `0.1.0.0`                        |
| Verified API baseline      | Emby Server `4.11.0.5`, .NET 8                      |
| License                    | GPL-3.0-only                                        |
| Website, after publication | `https://ultreia-io.github.io/emby-library-hub/`    |
| Thumbnail concept          | [16:9 banner](assets/banner.svg)                    |

### Short description

Browse your libraries, read private daily archives, and receive updates in French or English.

### Overview draft

Library Hub records additions and removals by library and calendar day.

Readers can browse a private archive or subscribe by email with confirmation, language choice, and unsubscribe controls.

Each subscriber receives at most one daily report. Administrators can rebuild HTML without resending mail.

The plugin includes archive reset with backup, configuration import/export, and subscriber management.

All signed-in Emby members share the saved archive. Browsing and emails respect current account permissions.

### Validation still required before submission

- Confirm runtime behavior on the final 0.1.0 server build, including signup and real subscriber delivery.
- Confirm the accepted server compatibility range with Emby; do not claim untested platforms.
- Confirm package identity, image format, and current submission requirements with the Emby team.
- Capture screenshots using synthetic data and export the 16:9 banner in the format the portal accepts.
- Review whether native clients need separate approval for the custom user-menu page.

No request, forum message, or package submission is sent by this repository's workflows.

# Préparer la proposition au catalogue Emby

Library Hub ne figure pas au catalogue officiel. Cette page prépare une future proposition par Tony.

Le guide Emby prévoit une identité de développeur obtenue auprès de son équipe, puis une fiche dans son portail.

La fiche demande notamment identifiant, descriptions, DLL cible, images et contraintes de version.

La version du catalogue doit correspondre à celle de l’assemblage. Vérifiez les règles en vigueur avant soumission.

Source : [guide officiel Emby](https://betadev.emby.media/doc/plugins/dev/Getting-your-plug-in-in-the-catalog.html).

## Fiche proposée

| Champ                  | Valeur préparée                                     |
| ---------------------- | --------------------------------------------------- |
| Nom                    | Library Hub                                         |
| Identifiant            | `f6975142-a690-4cdc-b98b-2c43b2d084ed`              |
| Cible                  | Emby Server                                         |
| Fichier                | `Emby.LibraryHub.dll`                               |
| Version                | `0.1.0` — assemblage `0.1.0.0`                      |
| API vérifiée           | Emby Server `4.11.0.5`, .NET 8                      |
| Licence                | GPL-3.0-only                                        |
| Site après publication | `https://ultreia-io.github.io/emby-library-hub/`    |
| Visuel proposé         | [Bannière 16:9](assets/banner.svg)                  |

### Description courte

Explorez vos médiathèques, consultez leurs archives privées et recevez leurs nouveautés en français ou en anglais.

### Présentation proposée

Library Hub enregistre les ajouts et suppressions par médiathèque et par jour civil.

Les lecteurs consultent les archives ou s’abonnent par courriel, avec confirmation, choix de langue et désabonnement.

Chaque abonné reçoit au maximum un rapport quotidien. Les administrateurs reconstruisent le HTML sans renvoyer de courriel.

Le plugin comprend une réinitialisation sauvegardée, l’import/export des paramètres et la gestion des abonnés.

Les archives sont communes à tous les membres connectés. Exploration et courriels respectent les droits du compte.

### Vérifications avant soumission

- Vérifier la version finale 0.1.0 sur le serveur, y compris inscription et envoi quotidien réel.
- Confirmer avec Emby les versions de serveur acceptées ; ne pas annoncer de plateformes non testées.
- Confirmer l’identifiant, les formats d’image et les exigences actuelles avec l’équipe Emby.
- Capturer des écrans avec des données fictives et exporter la bannière au format accepté.
- Vérifier si les clients natifs exigent une validation séparée de la page du menu utilisateur.

Aucun workflow ne contacte l’équipe Emby, ne publie de message ou ne soumet de paquet au catalogue.

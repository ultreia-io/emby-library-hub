# Sauvegarde et restauration

Remplacer la DLL conserve la configuration, l’historique suivi, les abonnés et les archives.

Arrêtez Emby avant une sauvegarde ou une restauration cohérente des fichiers.

## Fichiers à conserver

Les chemins ci-dessous sont relatifs au dossier de données d’Emby.

| Chemin                                          | Contenu                                                                      |
| ----------------------------------------------- | ---------------------------------------------------------------------------- |
| `plugins/configurations/Emby.LibraryHub.xml`    | Paramètres et identifiants SMTP                                              |
| `data/library-hub/state.json`                   | Inventaire et historique observé                                             |
| `data/library-hub/subscribers.json`             | Abonnés, jetons, progression                                                 |
| `data/library-hub/reports/`                     | Pages HTML, identifiant de partage, index et sauvegardes de réinitialisation |

Sauvegardez cet ensemble de dossiers au même moment. L’export JSON ne contient ni les abonnés ni les archives.

Les sauvegardes contiennent des données privées. Limitez leurs droits d’accès et évitez les emplacements publics.

## Écriture atomique

Les mises à jour écrivent un fichier temporaire puis remplacent l’ancien, avec conservation d’une copie `.bak`.

Si le fichier principal manque mais qu’une sauvegarde existe, le plugin signale une erreur au lieu de repartir de zéro.

Consultez le journal et restaurez un ensemble cohérent pendant l’arrêt du serveur.

Ne supprimez jamais `subscribers.json` pour corriger une erreur de courriel : vous perdriez les consentements et le suivi.

Une ancienne sauvegarde peut rétablir des abonnements et une progression obsolètes. Vérifiez-les avant de reprendre les envois.

## Annuler une réinitialisation des archives

Les sauvegardes sont dans `data/library-hub/reports/reset-backups/<timestamp-id>/`.

Pendant l’arrêt d’Emby, restaurez `archive.json`, `index.html` et les pages quotidiennes de la même sauvegarde.

Restaurez ensemble l’index et les rapports quotidiens pour garder une liste cohérente avec les fichiers.

Redémarrez Emby et ouvrez un rapport connu depuis l’index privé.

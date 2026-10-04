# Installer Library Hub 0.1.0

Library Hub ajoute à Emby un catalogue dans le navigateur, des archives HTML quotidiennes et des abonnements facultatifs par courriel.

## Avant de commencer

- Cible : **Emby Server 4.11.0.5 avec .NET 8**. Le développement utilise Synology ; les autres versions restent à valider.
- Vous devez administrer Emby et pouvoir écrire dans son dossier de plugins.
- Prévoyez une interruption : redémarrer Emby coupe les lectures en cours.
- Le serveur SMTP sert uniquement aux abonnements. Exploration et archives fonctionnent sans courriel.
- Les lecteurs utilisent un compte Emby actif et un navigateur, même s'ils regardent habituellement sur Android TV.

**Accès aux archives :** tous les membres connectés voient les mêmes rapports, toutes médiathèques rapportées comprises.
L'exploration et les courriels respectent les droits Emby de chaque membre.
La sélection de médiathèques par l'administrateur concerne seulement l'exploration.

## 1. Télécharger

Ouvrez la [page des versions GitHub](https://github.com/ultreia-io/emby-library-hub/releases).
Téléchargez **emby-library-hub-0.1.0.zip** et **SHA256SUMS** parmi les fichiers de la version 0.1.0.
Choisissez le ZIP du plugin, pas les archives de sources générées automatiquement par GitHub.

Si aucune version n'est encore publiée, construisez ces fichiers localement avec `./tools/package`.
L'installation normale ne demande ni SDK .NET, ni Node.js, ni Python, ni copie des sources.

Sous Linux, vérifiez le ZIP depuis le dossier contenant les deux fichiers :

```sh
sha256sum --check --ignore-missing SHA256SUMS
```

Le résultat du ZIP doit être **OK**. Extrayez-le et repérez **Emby.LibraryHub.dll**.
La DLL contient ses dépendances de messagerie. Installez seulement cette DLL, sans assemblages Emby ni DLL de messagerie supplémentaires.

## 2. Installer sur le serveur

Library Hub n'est pas encore dans le catalogue officiel Emby. Ce catalogue ne permet pas d'importer une DLL locale.

### Synology

1. Arrêtez **Emby Server** dans le **Centre de paquets** DSM.
2. Copiez `Emby.LibraryHub.dll` dans `/var/packages/EmbyServer/var/plugins/`.
3. Donnez au compte de service Emby le droit de lire le fichier ; le mode `644` convient si les dossiers parents sont accessibles.
4. Démarrez **Emby Server** depuis le Centre de paquets.
5. Actualisez Emby Web avec **Ctrl+Maj+R** et connectez-vous avec un compte administrateur.
6. Ouvrez **Tableau de bord → Avancé → Library Hub**.

Utilisez File Station ou SSH avec un compte autorisé à écrire dans le dossier du paquet.
Si la copie indique **Permission denied**, corrigez les permissions ; ne poursuivez pas avec un remplacement incomplet.

### Autres installations

Repérez le dossier de données de l'instance Emby active, puis son sous-dossier `plugins`.
Arrêtez Emby, copiez la DLL en autorisant sa lecture par le service, puis redémarrez Emby.
Avec un conteneur, utilisez son volume de données persistant.
Ne copiez pas la DLL dans le dossier d'une application cliente Emby.

## 3. Configurer l'exploration et les archives

Dans les paramètres **Library Hub** :

1. Dans **Exploration du catalogue**, cochez les médiathèques à proposer aux membres.
2. Choisissez la **Langue des rapports et de l’aperçu** et le **Fuseau horaire** des rapports.
3. Renseignez l'**Adresse publique du serveur Emby**, par exemple `https://media.example.com`.
4. Laissez les deux options d'envoi désactivées pour utiliser uniquement l'exploration et les archives.
5. Cliquez sur **Enregistrer**.

Utilisez l'adresse de base du serveur, sans `/web/index.html`, lien vers un média ni jeton d'accès.
Ajoutez un préfixe de reverse proxy uniquement s'il appartient réellement à l'adresse de votre serveur.

Les paramètres s'appliquent immédiatement. Gardez la tâche planifiée **Library Hub** active, toutes les cinq minutes par défaut.

Ouvrez **Explorer les médiathèques**, **Rechercher dans les médiathèques** et **Ouvrir les archives privées** depuis les paramètres.
Copiez le lien d'archives, qui exige une connexion Emby, pour partager le site autonome.
Les membres utilisent sa navigation **Archives**, **Explorer**, **Rechercher** et **Mon abonnement**.

## 4. Activer les abonnements par courriel (facultatif)

1. Saisissez l'expéditeur, le serveur SMTP, le port, l'identifiant et le mot de passe fournis par votre messagerie.
2. Sur le port **465**, TLS est automatique. Sur le port **587**, activez **Utiliser STARTTLS sur les ports autres que 465**.
3. Réglez l'**Heure d’envoi** et vérifiez le **Fuseau horaire**.
4. Cochez **Activer les inscriptions de la communauté et l’envoi aux abonnés**, puis enregistrez.
5. Ouvrez **Mon abonnement**, saisissez votre adresse et demandez une confirmation.
6. Suivez le lien reçu, connectez-vous avec le même compte Emby si nécessaire, puis cliquez sur **Confirmer mon abonnement**.
7. Cochez **Activer les courriels quotidiens aux abonnés**, puis enregistrez pour commencer les envois quotidiens.

L'administrateur s'abonne comme les autres membres. Chaque personne choisit le français ou l'anglais pour ses courriels.
La langue des archives et celle de l'interface Emby sont indépendantes.

Seules les journées terminées avec des changements donnent lieu à un courriel. Le suivi commence au jour de la confirmation.
L'envoi intervient après l'heure configurée, avec au maximum un rapport enregistré comme envoyé par adresse et par jour.
Générer du HTML ou afficher un aperçu n'envoie aucun courriel.

## 5. Vérifier l'installation

- L'exploration affiche les médiathèques proposées par l'administrateur et autorisées pour le membre connecté.
- Recherchez avec au moins deux caractères, ou choisissez un autre critère, puis ouvrez un résultat dans Emby.
- L'aperçu du rapport du jour affiche les changements suivis, ou indique leur absence.
- Une archive vide lors de la première installation est possible ; cela ne signifie pas que l'installation a échoué.
- Pour publier des ajouts antérieurs, choisissez une courte période **Du/Au** au format **AAAA/MM/JJ**, puis **Générer le HTML uniquement**.
- Si les courriels sont activés, vérifiez que votre adresse apparaît comme **Abonné** après confirmation.

Les ajouts historiques utilisent les dates enregistrées par Emby. Les suppressions antérieures au suivi ne sont pas reconstituables.
Ne réinitialisez pas une archive simplement parce qu'elle est vide ; vérifiez d'abord les dates et l'historique suivi.

## Mettre à jour ou retirer le plugin

Avant une mise à jour, arrêtez Emby et sauvegardez la DLL, les paramètres et tout le dossier `data/library-hub/`.
Sur Synology, les chemins ci-dessous sont relatifs à `/var/packages/EmbyServer/var/` :

| Chemin                                       | Contenu                                |
| -------------------------------------------- | -------------------------------------- |
| `plugins/Emby.LibraryHub.dll`                | Plugin installé                        |
| `plugins/configurations/Emby.LibraryHub.xml` | Paramètres, dont les identifiants SMTP |
| `data/library-hub/`                          | Historique, abonnements et archives    |

Remplacez seulement la DLL, démarrez Emby et actualisez le navigateur. Gardez les anciennes DLL hors du dossier `plugins` actif.
Paramètres, abonnés et archives sont conservés ; aucun import JSON n'est nécessaire à chaque mise à jour.
Pour revenir en arrière, arrêtez Emby avant de restaurer la DLL précédente et sa sauvegarde de données cohérente.

Pour retirer le plugin, arrêtez Emby, retirez sa DLL, puis redémarrez Emby.
Gardez les sauvegardes si vous envisagez une réinstallation. Le retrait arrête les traitements planifiés du plugin.

## Aide

[Configuration](https://ultreia-io.github.io/emby-library-hub/fr/configuration/) ·
[Dépannage](https://ultreia-io.github.io/emby-library-hub/fr/troubleshooting/) ·
[Sauvegarde et restauration](https://ultreia-io.github.io/emby-library-hub/fr/backup/)

Ne partagez jamais d'export de configuration, mot de passe SMTP, fichier d'abonnés ou journal d'authentification non anonymisé.

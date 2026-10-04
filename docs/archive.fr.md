# Archives HTML quotidiennes

Le lien des archives ouvre directement la liste des rapports HTML. **Mon abonnement** ouvre la page d’abonnement.

Les archives regroupent les journées publiées par mois. Chaque rapport présente les ajouts et suppressions par médiathèque.

Aujourd’hui reste indiqué comme incomplet jusqu’au jour suivant.

## Liens permanents autonomes

- Archives : `https://media.example.com/emby/LibraryHub/Archive`
- Rapport quotidien : `https://media.example.com/emby/LibraryHub/Archive/2026-10-03`
- Gestion de l’abonnement : `https://media.example.com/emby/LibraryHub/Subscriptions`

Ces pages sont autonomes, hors du tableau de bord Emby. Si nécessaire, connectez-vous directement avec vos identifiants Emby.

La connexion Emby Web est réutilisée lorsqu’elle est disponible sur la même origine navigateur et le même serveur.
Sinon, « Rester connecté sur ce navigateur » conserve la connexion entre onglets et liens reçus par courriel.
Décochez cette option sur un navigateur partagé. Un autre navigateur, appareil ou domaine peut demander une connexion.

Après connexion, la page demandée apparaît sans modifier son lien permanent. Les utilisateurs Android TV ouvrent ces liens sur téléphone ou ordinateur.

Les archives et les abonnements ont des pages distinctes. La consultation lit le HTML enregistré, sans reconstruire les rapports.


Ces adresses sont des exemples. Copiez les liens fournis par les paramètres du plugin pour respecter le préfixe de votre serveur.

## Consulter et partager

Utilisez **Ouvrir les archives privées** dans les paramètres ou **Nouveautés / Library updates** dans Emby Web.

Les applications Android et TV peuvent masquer les pages des plugins. Ouvrez alors la même adresse dans un navigateur.

Un compte Emby actif est requis. Tous les membres lisent les mêmes rapports enregistrés, toutes médiathèques comprises.

Les archives donnent accès au formulaire d’inscription. Le visionnage nécessite un compte Emby.

## Générer des rapports anciens

Choisissez les dates **Du** et **Au**, au format **AAAA/MM/JJ**, puis cliquez sur **Générer le HTML uniquement**.

Les deux dates sont incluses. La date de fin ne peut pas dépasser aujourd’hui dans le fuseau configuré.

La génération modifie seulement le HTML. Elle n’envoie aucun courriel et ne modifie pas la progression des abonnés.

## Réinitialiser et reconstruire

1. Cliquez sur **Réinitialiser la liste des archives**.
2. Choisissez la période à conserver.
3. Cliquez sur **Générer le HTML uniquement**.

Une sauvegarde privée est créée avant d’effacer l’index privé.

Les paramètres, abonnés, changements suivis et identifiant de partage sont conservés.

La publication automatique reprend au jour de la réinitialisation. Les jours anciens nécessitent une génération explicite.

Un ancien lien quotidien renvoie une erreur 404 jusqu’à sa régénération. Les liens d’archives et d’inscription restent stables.

Les fichiers HTML retirés de l’index restent sur disque, mais ne sont plus servis par l’API publique.

## Origine des dates historiques

Les ajouts et suppressions observés utilisent la date de l’événement enregistré par le plugin.

Pour les ajouts anciens, une génération explicite peut utiliser les métadonnées `DateCreated` d’Emby.

Ces valeurs peuvent refléter une ancienne date de fichier plutôt que son ajout récent au serveur.

Une nouvelle génération reprend les mêmes dates. La réinitialisation ne modifie pas les métadonnées Emby.

Une série peut apparaître plusieurs jours si ses épisodes ont des dates différentes.

Les épisodes sont regroupés par série et saison. Les numéros de saison inconnus sont omis, jamais inventés.

Les suppressions antérieures au suivi ne peuvent pas être reconstituées.

Les courriels aux abonnés n’utilisent pas le complément historique issu du catalogue.

Dans les courriels, les titres supprimés sont omis si leurs droits détaillés sont invérifiables. Voir [Confidentialité et sécurité](security.md).

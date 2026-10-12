# Administration des archives

Générez des rapports anciens, réinitialisez la liste et comprenez les dates de l’historique.

[Guide d’administration](administration.md) · [Guide de lecture des archives](archive.md)

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

# Architecture

## Historique partagé, archives enregistrées

Les événements Emby et la réconciliation de l’inventaire alimentent un historique persistant.

La génération explicite et la publication planifiée maintiennent les fichiers HTML et leur index.

La consultation lit ces fichiers après authentification Emby, sans requête média ni reconstruction.

Les archives sont communes à tous les membres connectés. Les courriels respectent les droits actuels du destinataire.

Les archives et la gestion des abonnements utilisent des pages et contrôleurs distincts.

## Catalogue dans le navigateur

Explorer et Rechercher interrogent Emby en lecture seule, selon le compte et les médiathèques proposées par l'administrateur.
La recherche lit les métadonnées par lots et vérifie les droits avant de renvoyer résultats ou groupes.
Séries, saisons et épisodes sont chargés à la demande ; aucune copie persistante du catalogue n'est créée.

## Authentification

La page de connexion et ses ressources fixes sont anonymes ; les services de données exigent une authentification.
Les routes de gestion exigent aussi le rôle administrateur.

Les routes membres lisent l’identité via `IAuthorizationContext` et refusent tout compte absent ou inactif.

Le client ne fournit aucun identifiant utilisateur. Une clé API de serveur sans utilisateur ne donne pas accès aux rapports.

La page utilise du JSON authentifié et un cadre `srcdoc` isolé, sans identifiants de connexion dans les liens.

Fichiers et sauvegardes restent privés sur le serveur ; la page anonyme ne contient ni rapport ni détail des membres.

## Abonnements liés aux comptes

Chaque entrée conserve son propriétaire Emby et un propriétaire en attente pour les nouvelles confirmations.

Les preuves de formulaire sont signées et liées au compte et à l’action. Les jetons sont hachés et expirent après 24 heures.

Seul le compte demandeur confirme. Un autre compte ne peut ni récupérer une adresse confirmée ni retirer son abonnement.

Seuls les abonnements confirmés liés à un compte Emby autorisé reçoivent des rapports.

## Calendrier et droits d’envoi

Les journées vont d’un minuit local au suivant, dans le fuseau configuré.

La tâche vérifie toutes les cinq minutes et traite au maximum 100 étapes par exécution, après l’heure choisie.

Chaque adresse conserve le prochain jour, l’identifiant du message en attente, la nouvelle tentative et le dernier succès.

Chaque tentative reconstruit le contenu selon les droits actuels ; une nouvelle tentative du même jour conserve son identifiant.

Un ancien corps de message ne contourne jamais les contrôles. Un rapport entièrement masqué est ignoré sans courriel.

Un succès limite l’abonné à un rapport par jour civil. Les journées manquées sont rattrapées dans l’ordre chronologique.

## Stockage et concurrence

Le coordinateur sérialise les modifications de l’inventaire et de l’historique. Un verrou distinct protège les abonnements.

Le message est enregistré avant SMTP. Un échec diffère l’adresse d’une heure ; les autres destinataires continuent.

Les remplacements de fichiers sont atomiques, avec sauvegarde. Un état principal absent ne remet jamais l’historique à zéro.

SMTP et la persistance locale ne sont pas atomiques ; une issue incertaine peut créer un doublon.

Génération et réinitialisation n’utilisent pas SMTP et ne modifient pas la progression des abonnés.

## Langues et dépendances

Les paramètres suivent la langue active d’Emby, indépendamment des rapports et de la langue choisie par chaque abonné.

MailKitLite et MimeKitLite sont intégrés. Seule la DLL du plugin est déployée.

Les tests vérifient l’isolation, les propriétaires, les changements de droits, le navigateur et les contrats API Emby réels.

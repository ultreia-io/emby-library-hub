# Confidentialité et sécurité

## Une communauté Emby fermée

Rapports, inscription, confirmation et désabonnement personnel exigent un compte Emby actif.

Partager un lien ne donne aucun accès à lui seul. Rapports et gestion des abonnements exigent une connexion.

L’identité vient de la session authentifiée d’Emby, jamais d’un identifiant utilisateur fourni par la requête.

Tous les membres Emby actifs et connectés lisent les mêmes rapports HTML enregistrés, toutes médiathèques comprises.

La consultation lit les fichiers existants : aucune reconstruction, analyse des médias ou émission de courriel.

Paramètres, génération, aperçu, réinitialisation et liste complète des abonnés restent réservés aux administrateurs.

## Accès aux courriels

Une adresse appartient au compte qui l’a demandée et confirmée. Un autre compte ne peut pas la récupérer.

Avant l’envoi, le compte doit exister, être actif, non verrouillé et dans sa plage horaire d’accès autorisée.

Les droits sont revérifiés à chaque préparation, y compris les nouvelles tentatives après échec SMTP.

Un ancien message enregistré ne constitue jamais une autorisation d’accès.

Chaque message a un destinataire. Révoquer un compte bloque les futurs rapports, sans rappeler les messages déjà reçus.

Les destinataires peuvent transférer leurs courriels. Inscrivez seulement des adresses que vous contrôlez et jugez fiables.

## Médias supprimés dans les courriels

Emby vérifie les médias existants. Un média supprimé ne permet plus de vérifier ses restrictions détaillées.

Son ancien titre apparaît seulement avec l’accès actuel à sa médiathèque et sans restriction au niveau des médias.

Les restrictions d’âge, d’étiquettes, de contenu non classé ou de sous-dossiers masquent les suppressions invérifiables.

Certains changements sont donc omis plutôt que de révéler un contenu dont l’accès ne peut pas être établi.

## Navigateur et identifiants

La page autonome utilise l’API de connexion Emby. Le mot de passe est envoyé uniquement au serveur puis effacé du formulaire.

« Rester connecté sur ce navigateur » est coché par défaut. Le jeton est conservé entre les onglets et les visites.
Décochez cette option pour limiter une nouvelle connexion à l’onglet courant. Le mot de passe n’est jamais enregistré.
Les jetons sont envoyés dans les en-têtes, jamais dans les URL.

Sans session Library Hub enregistrée, la page peut réutiliser le compte sélectionné dans Emby Web sur la même origine.
L’identifiant du serveur doit correspondre ; aucun autre utilisateur enregistré n’est sélectionné.
Le jeton Emby réutilisé n’est pas copié dans le stockage permanent.
Une déconnexion ou un changement de compte Emby actualise les pages utilisant cette session.

La déconnexion efface les sessions Library Hub des autres onglets et demande la révocation du jeton à Emby.
Cela concerne également un jeton réutilisé depuis Emby Web.
Elle désactive la connexion automatique via Emby jusqu’à la prochaine connexion manuelle à Library Hub.
Une session révoquée ramène au formulaire de connexion. Un stockage navigateur restreint peut imposer une reconnexion.
Quitter la page efface les données privées affichées.

Seuls la page de connexion et le JavaScript fixe sont publics. Le HTML des rapports et les abonnements exigent une API authentifiée.

Une politique CSP restrictive et un cadre sans scripts isolent le HTML enregistré. Les réponses privées utilisent `Cache-Control: no-store`.


## Signaler une vulnérabilité

Avant publication, utilisez le canal privé convenu avec le responsable ; ensuite, le signalement privé GitHub.

Ne publiez jamais d’identifiants, d’abonnés, de liens privés ou d’en-têtes d’authentification dans une issue.

Le site de documentation reste public et utilise seulement des exemples génériques.

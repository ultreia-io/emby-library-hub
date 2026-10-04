# Démarrage rapide

Commencez par l'exploration et les archives ; activez les courriels seulement si vous souhaitez une liste de diffusion.

## Administrateur

1. [Téléchargez et installez](INSTALL.md) la DLL, redémarrez Emby, puis ouvrez **Tableau de bord → Avancé → Library Hub**.
2. Choisissez les médiathèques proposées, la langue des rapports, le fuseau horaire et l'URL publique. Enregistrez.
3. Ouvrez **Explorer les médiathèques** pour vérifier le catalogue. Utilisez **Rechercher** pour les titres ou d'autres métadonnées.
4. Ouvrez les archives privées et partagez leur lien avec vos membres Emby.
5. Gardez la tâche planifiée **Library Hub** active. Elle s'exécute toutes les cinq minutes par défaut.

Tous les membres connectés voient les mêmes archives, toutes médiathèques rapportées comprises.
L'exploration et les courriels respectent les droits Emby de chaque membre.

Pour une archive neuve, générez une courte période si vous souhaitez retrouver des ajouts antérieurs.
Utilisez des dates **AAAA/MM/JJ** et **Générer le HTML uniquement** ; aucun courriel n'est envoyé.

## Liste de diffusion facultative

1. Configurez [SMTP et l'heure d'envoi](configuration.md), puis activez les abonnements de la communauté et enregistrez.
2. Ouvrez **Mon abonnement**, saisissez votre adresse, choisissez une langue et demandez une confirmation.
3. Suivez le lien reçu avec le même compte Emby et cliquez sur **Confirmer mon abonnement**.
4. Activez les courriels quotidiens aux abonnés et enregistrez lorsque vous êtes prêt.

Chaque adresse confirmée reçoit au maximum un rapport enregistré comme envoyé par jour civil ; les jours vides sont ignorés.
Les rapports couvrent les journées terminées dans le fuseau choisi. Les confirmations sont des courriels distincts.

## Membres

Ouvrez le lien partagé dans un navigateur et connectez-vous avec votre compte Emby habituel.
La connexion Emby Web peut être réutilisée sur la même origine ; sinon, la page propose de rester connecté sur ce navigateur.

Utilisez **Explorer** pour le catalogue sélectionné ou **Rechercher** pour les métadonnées et les autres critères.
Ouvrez **Archives** pour les changements quotidiens et **Mon abonnement** pour vous abonner ou vous désabonner.

Les utilisateurs d'Android TV peuvent ouvrir le même lien sur téléphone ou ordinateur. Les liens de lecture ouvrent Emby.

## Avant une mise à jour

Sauvegardez les paramètres et tout le dossier `data/library-hub/` pendant l'arrêt d'Emby.
Remplacer seulement la DLL conserve les paramètres, les abonnés et les rapports.

[Configuration](configuration.md) · [Exploration](browse.md) · [Dépannage](troubleshooting.md) · [Sauvegarde](backup.md)

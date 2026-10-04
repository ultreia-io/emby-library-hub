# Dépannage

## Le plugin ou ses paramètres sont absents

Vérifiez que seule la DLL `Emby.LibraryHub.dll` est copiée dans le dossier des plugins, puis redémarrez Emby.

Consultez **Tableau de bord → Extensions** et **Avancé → Library Hub**. Actualisez avec Ctrl+Maj+R.

Les paramètres sont réservés aux administrateurs. Les archives privées utilisent une page distincte.

## Les paramètres restent en anglais

Choisissez le français dans les préférences d’affichage d’Emby, puis rouvrez les paramètres du plugin.

Après une mise à jour de la DLL, redémarrez Emby et actualisez son interface avec Ctrl+Maj+R.

La langue du rapport contrôle le contenu des archives et de l’aperçu, pas la langue de l’interface.

## Android TV n’affiche aucun menu du plugin

Certains clients masquent les pages personnalisées. Ouvrez le lien des archives ou d’inscription sur votre téléphone.

Cela n’affecte pas l’envoi quotidien aux abonnés.

## Le courriel d’inscription n’arrive pas

Vérifiez l’adresse, les indésirables, l’autorisation de l’expéditeur, les identifiants SMTP et le mode TLS.

Vous pouvez redemander un lien immédiatement, y compris après un désabonnement.
Utilisez le dernier courriel : les anciens liens en attente sont remplacés.

En cas d’échec d’envoi ou d’adresse liée à un autre compte, la page affiche une erreur.
L’administrateur peut consulter le journal Emby pour les erreurs d’envoi.

## Les rapports quotidiens n’arrivent pas

Vérifiez dans cet ordre :

1. L’adresse est à l’état **Abonné**, et non en attente de confirmation.
2. Les inscriptions de la communauté et les courriels quotidiens sont tous deux activés.
3. L’heure enregistrée est passée dans le fuseau configuré.
4. Des jours terminés contiennent des changements observés depuis la confirmation.
5. L’abonné n’a pas déjà reçu un rapport aujourd’hui.
6. La tâche planifiée est active et l’état de l’abonné n’indique pas d’échec SMTP.

Les journées sans changement ne produisent aucun courriel. Un échec est réessayé après une heure.

Un ancien rapport en attente est envoyé avant les suivants, toujours dans la limite d’un rapport par jour.

## La génération HTML affiche des dates anciennes surprenantes

Comparez les dates avec les métadonnées de création d’Emby. Elles peuvent refléter les dates des fichiers.

Une série répétée peut correspondre à des épisodes différents. Une saison inconnue est omise du regroupement.

Réinitialisez les archives et régénérez une période plus récente pour ne plus publier ces anciens éléments.

## Un lien de rapport renvoie une erreur 404

Vérifiez que l’adresse de partage est complète et que la journée est publiée.

Après réinitialisation, un ancien lien fonctionne de nouveau seulement lorsque sa journée est régénérée.

Restaurez ensemble `archive.json` et ses pages HTML quotidiennes depuis la même sauvegarde.

## Une opération échoue

Consultez le journal Emby à l’heure de l’erreur. Sur Synology, il est généralement dans le dossier `var/logs/` du paquet.

Un démarrage ou une analyse des médiathèques peut différer la réconciliation. Réessayez après la fin de l’analyse.

Un fichier d’état absent ou corrompu nécessite une restauration, pas la suppression du dossier de données.

Pour signaler un défaut, indiquez les versions du plugin et d’Emby, le système, les étapes et une trace anonymisée.

Retirez les jetons, liens de partage, identifiants SMTP, adresses des abonnés et chemins des médias avant tout partage.

Dans les courriels, les titres supprimés sont omis si leurs droits détaillés sont invérifiables. Voir [Confidentialité et sécurité](security.md).

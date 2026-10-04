# Configuration

Ouvrez **Tableau de bord → Avancé → Library Hub** dans l’interface web Emby.

Cliquez sur **Enregistrer** après vos modifications. Aucun redémarrage du serveur n’est nécessaire.

## Exploration du catalogue

Choisissez les médiathèques proposées aux membres, dans la limite de leurs droits Emby.

Toutes sont proposées initialement. Enregistrer conserve les cases cochées ; tout décocher masque le catalogue.

Après enregistrement, les nouvelles médiathèques doivent être cochées ici pour être proposées.

Chaque membre peut réduire sa sélection. Sans filtre, Explorer ouvre tout son catalogue sélectionné.

Ce choix est inclus dans l’export et la restauration JSON. Il ne modifie ni les archives ni les courriels.

## Langue de l’interface

Le plugin suit la langue d’affichage active d’Emby, y compris votre préférence utilisateur.

Le français et ses variantes régionales sont pris en charge. Les autres langues utilisent l’anglais.

Les libellés, aides, boutons, messages et états des abonnés sont traduits.

Ce choix ne modifie ni la langue des archives ni celle choisie par chaque abonné.

## Envoi et langue des rapports

| Paramètre                          | Rôle                                                                  |
| ---------------------------------- | --------------------------------------------------------------------- |
| Courriels quotidiens aux abonnés   | Active les rapports planifiés aux abonnés confirmés                   |
| Inscriptions de la communauté      | Active les inscriptions et les envois aux abonnés                     |
| Heure d’envoi                      | Heure locale à partir de laquelle la tâche peut envoyer un rapport    |
| Fuseau horaire                     | Limites de la journée civile, par exemple `Europe/Paris` ou `Etc/UTC` |
| Langue des rapports et de l’aperçu | Français ou anglais pour les pages HTML et l’aperçu administrateur    |

Chaque abonné choisit la langue de ses courriels indépendamment de celle des archives.

Les deux options doivent être activées pour les rapports quotidiens.

Les confirmations d’inscription peuvent être envoyées même si l’envoi quotidien est désactivé.

Désactiver les inscriptions suspend les nouveaux abonnements et les rapports. Le désabonnement reste disponible.

## Adresse du serveur

Renseignez l’adresse de base accessible depuis l’extérieur, par exemple `https://media.example.com`.

N’ajoutez pas `/web/index.html`, d’identifiants, de paramètres d’URL ou de lien vers un média.

Incluez un préfixe de proxy inverse seulement s’il appartient réellement à l’adresse publique du serveur.

Testez le lien des archives hors de votre réseau domestique avant de le partager.

**Inclure les liens vers les vidéos ajoutées** contrôle les liens de lecture. Les médias supprimés n’en ont pas.

## SMTP

| Paramètre    | Exemple                                                                   |
| ------------ | ------------------------------------------------------------------------- |
| Serveur      | `smtp.example.com`                                                        |
| Expéditeur   | `library@example.com`                                                     |
| Port         | `465` pour TLS implicite ; généralement `587` pour STARTTLS               |
| Identifiant  | Identifiant du compte SMTP                                                |
| Mot de passe | Mot de passe SMTP ou mot de passe d’application fourni par le prestataire |

Le port 465 démarre toujours avec TLS. Pour les autres ports, activez STARTTLS si votre prestataire le prend en charge.

L’authentification SMTP sans TLS est refusée. La vérification des certificats reste active.

Le prestataire doit autoriser l’adresse d’expédition. Chaque abonné est un destinataire individuel.

## Aperçu et dates

**Aperçu du rapport d’aujourd’hui** actualise le suivi et affiche le contenu sans envoyer de courriel.

Les champs affichent **AAAA/MM/JJ**. Le bouton **Calendrier** ouvre le sélecteur du navigateur s’il est disponible.

La génération inclut les deux dates et refuse les jours futurs dans le fuseau enregistré.

## Sauvegarde et restauration

**Exporter en JSON** télécharge la configuration enregistrée, pas les modifications en cours.

L’export contient le mot de passe SMTP. Gardez-le hors des dépôts Git et des demandes d’assistance publiques.

**Restaurer depuis un fichier JSON** valide et enregistre immédiatement les paramètres.

Un fichier invalide laisse la configuration inchangée.

Les abonnés et l’historique sont sauvegardés séparément : voir [Sauvegarde et restauration](backup.md).

Remplacer la DLL conserve les paramètres. Importez du JSON seulement pour remplacer volontairement la configuration.

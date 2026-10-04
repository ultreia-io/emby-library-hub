# Abonnements de la communauté

Partagez le lien des archives depuis les paramètres. **Mon abonnement** ouvre l’inscription ; **Ouvrir les archives privées** revient aux rapports.

## S’inscrire avec son compte Emby

1. Ouvrez le lien de communauté ou **Nouveautés / Library updates** dans le menu utilisateur Emby Web.
2. Connectez-vous avec votre propre compte Emby.
3. Cliquez sur **Mon abonnement**, saisissez votre adresse électronique et choisissez **Français** ou **English**.
4. Demandez une confirmation et ouvrez le lien reçu dans les 24 heures.
5. Connectez-vous avec le même compte et cliquez sur **Confirmer mon abonnement**.

Les utilisateurs d’Android TV peuvent utiliser leur téléphone ou ordinateur avec leur compte Emby habituel.

Ouvrir le lien ne modifie rien. Cela protège contre les scanners automatiques des courriels.

Un autre compte ne peut ni confirmer votre demande, ni récupérer votre adresse, ni retirer votre abonnement.

## Gérer son abonnement

Votre page affiche seulement vos abonnements. Cliquez sur **Désabonner** à côté de votre adresse.

Chaque rapport contient aussi un lien de désabonnement avec connexion Emby, puis confirmation de l’action.

Le désabonnement reste disponible si les inscriptions sont suspendues. Un administrateur peut aussi retirer une adresse.

Pour changer de langue, demandez une nouvelle confirmation pour la même adresse depuis le même compte.

L’ancienne langue reste active jusqu’à confirmation. La nouvelle s’applique aux rapports nouvellement préparés.

## Pour les administrateurs

Partagez le lien dans **Rapports de la communauté privée**. Les lecteurs doivent déjà posséder un compte Emby.

**Actualiser les abonnés** affiche adresse, langue, confirmation et erreurs. **Désabonner** retire une entrée.

L’administrateur s’inscrit aussi depuis la page des membres ; il n’y a pas de destinataire administrateur séparé.

## Envoi quotidien

- Le suivi commence au jour civil de la confirmation liée au compte.
- Seules les journées terminées avec des changements autorisés pour ce compte produisent un courriel.
- Chaque adresse confirmée reçoit au maximum un rapport enregistré comme envoyé par jour civil local.
- Les rapports manqués sont rattrapés à raison d’un par jour d’envoi, du plus ancien au plus récent.
- Un échec SMTP est réessayé après une heure, sans bloquer les autres adresses.
- Compte et droits sur les médias sont revérifiés avant chaque tentative, y compris les envois en attente.
- Un compte supprimé, désactivé, verrouillé ou hors de sa plage d’accès ne reçoit aucun rapport.
- Les confirmations sont distinctes des rapports quotidiens.

Générer, réinitialiser ou prévisualiser les archives ne déclenche aucun envoi et ne remet pas la progression à zéro.

SMTP et la sauvegarde locale ne sont pas atomiques ; une issue incertaine peut encore produire un doublon.

## Demandes de confirmation

Chaque demande envoie immédiatement un nouveau lien, y compris après un désabonnement.
Le dernier lien remplace les liens en attente pour cette adresse, sans créer d’abonnement en double.
Un abonnement actif et sa progression d’envoi sont conservés jusqu’au désabonnement.

La page affiche les erreurs de saisie, de compte et d’envoi au lieu d’ignorer silencieusement les demandes.
Une adresse liée à un autre compte Emby ne peut pas être réattribuée lors de l’inscription.

Les confirmations expirent après 24 heures ; les anciennes demandes sont nettoyées lors de requêtes ultérieures.
Une exécution planifiée traite au maximum 100 étapes de la file d’envoi.

Consultez [Confidentialité et sécurité](security.md) pour les restrictions sur les médias supprimés.

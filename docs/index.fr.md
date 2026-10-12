# Explorez vos médiathèques, suivez leurs nouveautés

![Library Hub](assets/banner.svg)

**Library Hub** réunit l’exploration des médiathèques, les archives quotidiennes privées et les courriels aux abonnés.

<div class="guide-paths" markdown>

<div class="guide-path" markdown>

### J’utilise Library Hub

Connectez-vous, explorez, recherchez, consultez les nouveautés et gérez votre abonnement.

[Ouvrir le guide utilisateur](user-guide.md){ .md-button .md-button--primary }

</div>

<div class="guide-path" markdown>

### J’administre le serveur

Installez le plugin, choisissez les accès, configurez les envois et protégez vos données.

[Ouvrir le guide d’administration](administration.md){ .md-button }

</div>

</div>

## Trouver quoi regarder

Recherchez dans plusieurs médiathèques, combinez les filtres et explorez par titre, date, note, réalisation ou interprète.
Dépliez les séries en saisons puis épisodes. [Découvrir l’exploration](browse.md).

## Des archives quotidiennes

Consultez les ajouts et suppressions par date. Ouvrez les films ou les pages des séries depuis chaque rapport.

Les épisodes sont regroupés par série et saison. L’index rassemble les journées par mois.

## Des abonnements choisis

Chaque lecteur choisit le français ou l’anglais, confirme son adresse et se désabonne quand il le souhaite.

Chaque personne reçoit un message individuel. Seuls les administrateurs peuvent consulter les adresses et leur état.

## Reconstruire sans renvoyer

Générez des rapports HTML anciens ou réinitialisez l’index sans modifier le suivi des envois aux abonnés.

Les courriels ont leur propre suivi persistant et sont limités à un rapport par jour civil et par abonné.

## Avant de commencer

- Base API : **Emby 4.11.0.5**. Le plugin cible **.NET Standard 2.0**.
- Courriels facultatifs : SMTP avec TLS sur le port 465 ou STARTTLS sur un autre port, par exemple 587.
- Une adresse publique du serveur pour les inscriptions et les liens des rapports.
- Une installation manuelle avec la DLL d’une version publiée.

Les liens exigent une connexion Emby. Les archives sont communes aux membres et incluent chaque médiathèque publiée.

L’exploration et les courriels respectent les droits Emby du membre.

[Configurer les envois](configuration.md) · [Gérer les abonnés](administration.md#4-activer-les-courriels-si-souhaite) · [Gérer les archives](archive-administration.md)

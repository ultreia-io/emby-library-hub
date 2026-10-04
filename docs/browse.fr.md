# Explorer les médiathèques

Ouvrez **Explorer** dans la navigation autonome, **Explorer / Library Hub** dans le menu utilisateur Emby Web ou le lien des paramètres du plugin.

Adresse permanente : `https://media.example.com/emby/LibraryHub/Browse`.

La page réutilise la connexion des archives et des abonnements. Chaque lecteur voit uniquement les médias autorisés par son compte Emby.

Ces adresses sont des exemples. Copiez les liens des paramètres pour respecter le préfixe de votre serveur.

## Explorer ou rechercher

**Explorer** ouvre le catalogue complet : choisissez les médiathèques, le tri, puis dépliez les groupes et les titres.

**Rechercher** dispose de sa propre page : `https://media.example.com/emby/LibraryHub/Search`.

Choisissez **Rechercher dans** (**Titre** par défaut) à côté du texte, ou utilisez les autres critères visibles.

Seules les médiathèques contenant des résultats sont affichées. Sans critère, la page invite à saisir une recherche.

Si rien ne correspond, un seul message remplace la liste des médiathèques vides.

Les deux pages partagent la sélection mémorisée, les droits Emby et la connexion.

La sélection est mémorisée sur ce navigateur. Initialement, toutes les médiathèques accessibles sont sélectionnées.
**Tout sélectionner** et **Tout désélectionner** concernent toute la liste accessible.

Explorer regroupe les médiathèques, le tri et **Afficher les épisodes** dans une carte toujours ouverte.

**Afficher les épisodes** est décoché par défaut. Les séries se déplient en saisons, liées à leur fiche Emby.

Cochez cette option pour déplier les saisons en épisodes. La décocher masque les épisodes sans replier la série.

Rechercher utilise une carte toujours ouverte : médiathèques, recherche par métadonnées, autres critères, puis tri.

**Afficher les épisodes** se trouve à côté du tri sur les deux pages, décoché par défaut.
Les dossiers d’une même série sont réunis selon leur identité Emby. Les saisons et épisodes suivent leurs métadonnées, y compris les sous-dossiers et les saisons virtuelles.
La saison 0 porte le nom **Hors-série**. Chaque série et saison dispose aussi d’un lien **Ouvrir dans Emby**.
La recherche exclut les épisodes par défaut : **Silo** affiche la série, que vous pouvez ensuite déplier.
Cochez **Rechercher aussi dans les métadonnées des épisodes** pour inclure les épisodes sous leur série et leur saison.

Cela active aussi **Afficher les épisodes**. Décocher cette dernière option désactive la recherche dans les épisodes.

Si le type de média est **Épisode**, décocher **Afficher les épisodes** remet le type sur **Tous**.
Le type **Épisode** active automatiquement cette option ; les autres types précis la désactivent.
**Effacer** désactive à nouveau cette option.
Un média présent dans plusieurs médiathèques sélectionnées apparaît sous chacune d’elles.

Le catalogue complet s’ouvre sans recherche. Les médiathèques sont dépliées automatiquement.

Les groupes et les titres se chargent à leur ouverture, par pages de 50 éléments.
**Afficher la suite** poursuit la liste de cette branche.
Les compteurs indiquent les éléments affichés, pas la taille totale de la médiathèque. Les médias interdits sont exclus.

Le texte recherché exige au moins deux caractères. Les autres critères peuvent être utilisés sans texte.

## Rechercher dans les métadonnées

Les médiathèques viennent en premier. Le sélecteur et le texte partagent une ligne, sauf sur les petits écrans.

Choisissez Titre, Toutes les métadonnées, Résumé, Interprètes, Réalisation, Genres, Étiquettes, Studios ou Année.

Titre comprend les titres affichés et originaux. Toutes les métadonnées réunit ces champs, avec les invités.

La recherche ignore la casse et les accents et recherche le texte saisi dans les métadonnées choisies.

Par exemple, choisissez Interprètes et saisissez `Gary Oldman`, puis limitez les résultats aux films non vus.

Les résultats hors titre indiquent leur origine : nom d’interprète ou court extrait du résumé, par exemple.

Tous les critères actualisent les mêmes résultats. **Effacer** conserve les médiathèques et le tri.

La recherche dans les épisodes est optionnelle. Le champ choisi s’applique aussi à leurs métadonnées.

## La médiathèque Collections

**Collections** apparaît une seule fois dans le sélecteur de médiathèques. Les collections restent à l’intérieur.
Dépliez par exemple **Collections → Alien Collection → Alien**. Les collections ne deviennent pas des médiathèques distinctes.
La liste se charge à l’ouverture de sa médiathèque, par ordre alphabétique et par pages de 50.
Dans Rechercher, seules les collections contenant des titres correspondants apparaissent.

La médiathèque Collections est masquée si aucune collection ne contient de résultat.
Le tri par titre affiche directement les médias. Les autres tris conservent leurs groupes à l’intérieur de la collection.
Les séries se déplient en saisons puis épisodes. Les droits Emby du membre restent appliqués à chaque titre.

## Trier

Le tri se trouve dans la carte de contrôle d’Explorer, et sous les filtres de Rechercher.

Les boutons de dépliage sous la carte concernent uniquement le catalogue.

Le tri choisi détermine les groupes dans chaque médiathèque :

| Tri            | Groupes                                                               |
| -------------- | --------------------------------------------------------------------- |
| Titre          | A–Z, 0–9, Autres                                                      |
| Date d’ajout   | Année → mois                                                          |
| Date de sortie | Année → mois                                                          |
| Année          | Décennie → année                                                      |
| Note           | Tranches entières : 8 ≤ note < 9, par exemple ; la dernière inclut 10 |
| Réalisation    | Lettre → cinéaste → titres                                            |
| Interprètes    | Lettre → interprète → titres                                          |

Les personnes sont regroupées selon leur nom complet dans Emby ; les accents sont ignorés pour la lettre initiale.
Les interprètes incluent les vedettes invitées. Un titre apparaît sous chaque personne créditée, une seule fois par personne.
Les noms proviennent uniquement des titres correspondants accessibles à votre compte.
Les crédits absents figurent dans **Inconnu**. Dépliez une personne pour voir ses titres, avec les mêmes filtres et pages.

Les lettres suivent le titre de tri Emby, avec sa gestion des articles et des accents.
Les dates d’ajout utilisent le fuseau horaire configuré dans le plugin.
Les métadonnées absentes figurent dans **Inconnu**, toujours en dernier. **Autres** reste aussi en dernier pour les titres.
La durée ne fait pas partie des critères de tri.

Choisissez l’ordre croissant ou décroissant. Les médiathèques restent classées alphabétiquement.

**Effacer** efface la recherche et les filtres en conservant la sélection, le critère de tri et son ordre.
**Déplier un niveau** ouvre le niveau suivant visible. **Tout replier** ferme toutes les branches chargées.

## Accès aux médiathèques

L’administrateur choisit les médiathèques proposées dans **Exploration du catalogue**, dans les paramètres du plugin.

Chaque membre peut en sélectionner une partie, limitée par ses droits Emby.

Ce choix concerne uniquement l’exploration. Les archives, les courriels et les autres accès Emby restent inchangés.

## Activité du serveur

L’exploration consulte la base Emby actuelle. Elle n’analyse aucun fichier média, ne génère aucune archive et n’envoie aucun courriel.
Les groupes sont calculés à la demande à partir des métadonnées correspondantes, lues par lots.
Ce calcul peut prendre plus de temps pour les grandes médiathèques. Aucun index persistant n’est créé.
Les éléments, saisons et épisodes se chargent à l’ouverture de leurs branches.
La saisie attend une courte pause avant la recherche. Le navigateur annule les requêtes obsolètes et en lance au plus trois simultanément.

Les liens des médias ne contiennent aucune clé API. Les requêtes utilisent la session authentifiée de la communauté.

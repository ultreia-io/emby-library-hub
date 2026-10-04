# Développement

## Prérequis

- SDK .NET 8 pour le plugin et les tests métier.
- Node.js 18 ou ultérieur pour les tests du contrôleur de paramètres.
- Python 3 pour le paquet ; Python 3.12 est utilisé pour le site.

```sh
./tools/check
./tools/package
```

Les dépendances NuGet sont verrouillées dans `packages.lock.json`. La restauration utilise le mode verrouillé.

`tools/check` compile, exécute les tests métier et d’interface, puis vérifie le chargement isolé du plugin.

Les tests SMTP utilisent des serveurs locaux. Ils ne contactent aucun prestataire ni destinataire réel.

## Organisation

| Dossier                           | Responsabilité                                           |
| --------------------------------- | -------------------------------------------------------- |
| `src/Emby.LibraryHub.Core/`       | Suivi, présentation, stockage, abonnements, SMTP et HTML |
| `src/Emby.LibraryHub/`            | Adaptation Emby, routes, tâche planifiée et paramètres   |
| `tests/Emby.LibraryHub.Tests/`    | Calendrier, persistance, envoi, HTML et SMTP             |
| `tests/ui/`                       | Contrôleurs et traduction avec une API Emby simulée      |
| `tests/PluginLoadSmoke/`          | Chargement isolé et contrats HTTP Emby                   |
| `docs/`                           | Site bilingue et guides d’installation                   |
| `.github/workflows/`              | Préparation de CI, Pages et des versions                 |

Les sources métier sont intégrées à l’assemblage du plugin. Une seule DLL est nécessaire à l’installation.

## Références correspondant au serveur

Le SDK par défaut correspond à la version Emby ciblée.

Pour une vérification supplémentaire, fournissez un dossier contenant les assemblages API du serveur cible.

```sh
dotnet build src/Emby.LibraryHub -c Release -p:EmbyReferencePath=/path/to/server-api
dotnet run --project tests/PluginLoadSmoke -c Release --   src/Emby.LibraryHub/bin/Release/net8.0/Emby.LibraryHub.dll /path/to/server-api
```

Ne versionnez ni ne redistribuez les DLL du serveur. Ce sont des références de test, pas des fichiers à déployer.

## Documentation bilingue

```sh
python3 -m venv .venv-docs
.venv-docs/bin/pip install -r docs/requirements.txt
.venv-docs/bin/mkdocs build --strict
python3 tools/check-docs.py
.venv-docs/bin/mkdocs serve
```

Les pages anglaises utilisent `.md`, les françaises `.fr.md`, avec les mêmes chemins et liens internes.

Le site anglais est à la racine, le français sous `/fr/`. Le sélecteur conserve la page en cours.

Le thème traduit la navigation et la recherche. Les polices sont celles du système, sans outil de mesure d’audience.

La configuration et le mécanisme de traduction sont décrits dans la [documentation officielle i18n](https://ultrabug.github.io/mkdocs-static-i18n/).

## Traduire les paramètres

Les textes du plugin sont centralisés dans `Configuration/locale.js`, avec des entrées françaises et anglaises.

Les pages utilisent des attributs `data-digest-*`. Le contrôleur applique les textes sans interpréter de HTML.

La langue active vient du module `globalize` d’Emby. Les tests couvrent les préférences, les variantes et le repli.

La langue d’affichage ne modifie pas celle des rapports ou des abonnés.

## Modifier le projet

1. Reproduisez le comportement avec des tests ciblés ou un cas d’exécution documenté.
2. Séparez les archives privées, l’envoi aux abonnés et les commandes administrateur.
3. Lancez `tools/check`, la compilation stricte du site et `tools/check-docs.py`.
4. Préparez le paquet et vérifiez-le sur un serveur de test avant une version publique.

Utilisez des adresses et médias fictifs. Ne versionnez jamais une configuration réelle ou un fichier d’abonnés.

[Architecture](architecture.md) · [Publication](releases.md) · [Sécurité](security.md)

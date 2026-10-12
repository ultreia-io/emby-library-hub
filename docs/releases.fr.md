# Versions et publication

GitHub Actions construit les versions étiquetées et prépare des brouillons.

Le dépôt est `ultreia-io/emby-library-hub`, avec `develop` comme branche par défaut.

La publication est une action distincte du responsable, après revue.

Les vérifications locales ne poussent rien, ne créent aucun tag et ne publient rien.

## Préparer une version localement

1. Définissez la version dans `Directory.Build.props`.
2. Ajoutez l’entrée du journal des changements et `docs/release-notes/<version>.md`.
3. Lancez les vérifications ci-dessous et examinez le paquet ainsi que la documentation bilingue.
4. Vérifiez le paquet sur le serveur Emby cible avant d’approuver la publication.

```sh
./tools/check
./tools/package
.venv-docs/bin/mkdocs build --strict
python3 tools/check-docs.py
python3 tools/check-package.py
python3 tools/check-package.py --tag-only "v<version>"
```

Remplacez `<version>` par la version définie dans `Directory.Build.props`.

L’option `--tag-only` vérifie le numéro et les notes de version. Elle ne crée aucun tag Git.

`Directory.Build.props` définit la version. Un tag doit être exactement `v` suivi de cette version.

Le code est sous GPL-3.0-only. Les dépendances intégrées conservent leurs propres notices.

## Workflows GitHub

| Workflow | Déclenchement                                    | Résultat                                                              |
| -------- | ------------------------------------------------ | --------------------------------------------------------------------- |
| CI       | Push sur develop, pull request, lancement manuel | Compilation, tests, vérification du site et paquet                    |
| Website  | Push sur develop, lancement manuel               | Compilation stricte bilingue, puis déploiement GitHub Pages           |
| Release  | Push d’un tag `v*`                               | Vérification, tests et création d’une version brouillon avec fichiers |

Les actions sont fixées par empreinte de commit. Les pull requests ne reçoivent aucun droit de publication.

Le workflow de publication refuse d’écraser une version existante.

## Publier une version approuvée

Après revue et réussite de la CI, créez et poussez le tag correspondant sur le commit validé.

Pour un tag nommé `v<version>`, le workflow joint :

- `Emby.LibraryHub-<version>.dll` pour l’installation manuelle.
- `emby-library-hub-<version>.zip` avec DLL, guides d’installation français et anglais, licence et notices.
- `emby-library-hub-<version>.sha256` pour les fichiers téléchargeables exacts.

Installez la DLL téléchargée sous le nom `Emby.LibraryHub.dll`, en remplaçant le fichier précédent.

Le brouillon utilise `docs/release-notes/<version>.md`.

Examinez le brouillon et ses fichiers avant de le publier manuellement.

Ne déplacez pas un tag publié et ne remplacez pas ses fichiers versionnés.

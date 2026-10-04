# Versions et publication

**État actuel : préparation locale de 0.1.0. Aucun dépôt GitHub, site ou version n’a été publié.**

Le dépôt prévu est `ultreia-io/emby-library-hub`, avec `develop` comme branche par défaut.

La publication est une action distincte du responsable, après revue locale.

## Vérifications locales

```sh
./tools/check
./tools/package
.venv-docs/bin/mkdocs build --strict
python3 tools/check-docs.py
python3 tools/check-package.py
```

Vérifiez le paquet sur le serveur Emby cible et relisez la documentation avant d’approuver la publication.

`Directory.Build.props` définit la version. Le tag doit être exactement `v` suivi de cette version.

Le code 0.1.0 est sous GPL-3.0-only. Les dépendances intégrées conservent leurs propres notices.

## Workflows GitHub préparés

| Workflow   | Déclenchement                                    | Résultat                                                              |
| ---------- | ------------------------------------------------ | --------------------------------------------------------------------- |
| CI         | Push sur develop, pull request, lancement manuel | Compilation, tests, site et paquet                                    |
| Website    | Push sur develop, lancement manuel               | Compilation stricte bilingue, puis déploiement GitHub Pages           |
| Release    | Push d’un tag `v*`                               | Vérification, tests et création d’une version brouillon avec fichiers |

Les actions sont fixées par empreinte de commit. Les pull requests ne reçoivent aucun droit de publication.

La publication d’une version reste manuelle : examinez le brouillon et ses fichiers avant de le rendre public.

Le workflow n’écrase jamais une version existante.

## Mise en place initiale, après accord

1. Créez le dépôt public dans `ultreia-io` et poussez les sources validées.
2. Définissez `develop` comme branche par défaut et activez GitHub Actions.
3. Dans **Settings → Pages**, choisissez **GitHub Actions** comme source.
4. Activez les signalements privés de vulnérabilités et les règles de revue des branches.
5. Actualisez les mentions de préparation locale et vérifiez les liens du site déployé.

L’adresse prévue est `https://ultreia-io.github.io/emby-library-hub/`, avec le français sous `/fr/`.

Ces étapes sont des instructions, pas des actions effectuées par une compilation locale.

## Première version

Après revue des sources et réussite de la CI, créez et poussez le tag `v0.1.0` sur le commit validé.

Le workflow joint :

- `Emby.LibraryHub.dll` pour l’installation manuelle et une future proposition au catalogue.
- `emby-library-hub-0.1.0.zip` avec DLL, guides d’installation français et anglais, licence et notices.
- `SHA256SUMS` pour les fichiers téléchargeables exacts.

Le brouillon utilise les notes de version de `docs/release-notes/`.

Ne déplacez pas le tag et ne changez pas les numéros après publication.

## Catalogue Emby

Une version GitHub n’ajoute pas automatiquement le plugin au catalogue Emby.

Préparez la [proposition au catalogue](emby-catalog.md) après validation du serveur avec la version finale.

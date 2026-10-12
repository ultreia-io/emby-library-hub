# Releases and publishing

GitHub Actions builds tagged releases and opens them as drafts.

The repository is `ultreia-io/emby-library-hub`, with `develop` as its default branch.

Publishing is a separate maintainer action after review. Local checks do not push, tag, or publish anything.

## Prepare a release locally

1. Set the version in `Directory.Build.props`.
2. Add the changelog entry and `docs/release-notes/<version>.md`.
3. Run the checks below and inspect the generated package and bilingual documentation.
4. Verify the package on the target Emby server before approving publication.

```sh
./tools/check
./tools/package
.venv-docs/bin/mkdocs build --strict
python3 tools/check-docs.py
python3 tools/check-package.py
python3 tools/check-package.py --tag-only "v<version>"
```

Replace `<version>` with the version from `Directory.Build.props`.

The `--tag-only` check validates the version string and notes. It does not create a Git tag.

`Directory.Build.props` is the version source. A release tag must be exactly `v` followed by that version.

The source license is GPL-3.0-only. Embedded dependencies retain their own notices.

## GitHub workflows

| Workflow | Trigger                                         | Result                                                               |
| -------- | ----------------------------------------------- | -------------------------------------------------------------------- |
| CI       | Push to develop; pull requests; manual dispatch | Build, tests, docs checks, package artifact                          |
| Website  | Push to develop; manual dispatch                | Strict bilingual docs build, then GitHub Pages deployment            |
| Release  | Push a `v*` tag                                 | Validate version, build and test, create a draft release with assets |

Actions are pinned to commit hashes. Pull requests do not receive publication credentials.

The release workflow refuses to overwrite an existing release.

## Publish an approved release

After review and successful CI, create and push the matching tag from the reviewed commit.

For a tag named `v<version>`, the workflow uploads:

- `Emby.LibraryHub-<version>.dll` for manual installation.
- `emby-library-hub-<version>.zip` with the DLL, English/French installation guides, license, and notices.
- `emby-library-hub-<version>.sha256` for the exact downloadable assets.

Install the downloaded DLL as `Emby.LibraryHub.dll`, replacing the previous file.

The draft release uses `docs/release-notes/<version>.md`.

Inspect the draft and its artifacts before manually publishing it.

Do not move a published tag or replace its versioned files.

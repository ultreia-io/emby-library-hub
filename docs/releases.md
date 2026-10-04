# Releases and publishing

**Current status: local preparation for 0.1.0. No GitHub repository, website, or release has been published.**

The planned repository is `ultreia-io/emby-library-hub`, with `develop` as its default branch.

Publishing is a separate maintainer action after local review.

## Local release checks

```sh
./tools/check
./tools/package
.venv-docs/bin/mkdocs build --strict
python3 tools/check-docs.py
python3 tools/check-package.py
```

Verify the package on the target Emby server and review the documentation before approving publication.

`Directory.Build.props` is the version source. The release tag must be exactly `v` followed by that version.

The 0.1.0 source license is GPL-3.0-only. Embedded dependencies retain their own notices.

## Prepared GitHub workflows

| Workflow   | Trigger                                         | Result                                                               |
| ---------- | ----------------------------------------------- | -------------------------------------------------------------------- |
| CI         | Push to develop; pull requests; manual dispatch | Build, tests, docs checks, package artifact                          |
| Website    | Push to develop; manual dispatch                | Strict bilingual docs build, then GitHub Pages deployment            |
| Release    | Push a `v*` tag                                 | Validate version, build and test, create a draft release with assets |

Actions are pinned to commit hashes. Pull requests do not receive publication credentials.

Release publication remains a manual step: inspect the generated draft and its artifacts before making it public.

The release workflow never overwrites an existing release.

## One-time repository setup, after approval

1. Create the public repository in `ultreia-io` and push the reviewed source.
2. Set the default branch to `develop` and enable GitHub Actions.
3. In **Settings → Pages**, choose **GitHub Actions** as the build source.
4. Enable private vulnerability reporting and select branch review requirements.
5. Update the publication status text and verify the deployed documentation links.

The intended Pages address is `https://ultreia-io.github.io/emby-library-hub/`.

These steps are instructions, not actions performed by a local build.

## Prepare the first release

After source review and successful CI, create and push `v0.1.0` from the reviewed commit.

The workflow uploads:

- `Emby.LibraryHub.dll` for manual installation and later catalog submission.
- `emby-library-hub-0.1.0.zip` with the DLL, English/French installation guides, license, and notices.
- `SHA256SUMS` for the exact downloadable assets.

The draft release includes the versioned release notes from `docs/release-notes/`.

Do not tag a different commit or change version numbers after publishing a release.

## Emby catalog

A GitHub release does not automatically create an Emby catalog listing.

Prepare and review the separate [catalog submission](emby-catalog.md) when the server build has been validated.

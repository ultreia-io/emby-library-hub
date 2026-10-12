# Regenerating documentation visuals

The English and French guides use committed SVG diagrams and PNG screenshots.

A normal MkDocs build reads these files directly. It does not run a browser or a .NET build.

## Diagrams

```sh
python3 tools/docs-diagrams.py
```

This creates three diagrams per language in `docs/assets/diagrams/`.

Each SVG has a title, description, and fixed layout. Review the rendered text before committing changed diagrams.

## Screenshots

Screenshots use the current plugin HTML and JavaScript, with fictional API responses and `.test` addresses.

The capture harness intercepts all browser requests. Unexpected requests and administrative writes fail the run.

No real server, account, subscriber store, or SMTP service is accessed.

Member screenshots use the standalone UI unchanged, including the real authentication and browsing controllers.

Archive samples are produced by `ReportHtml.Day` in the compiled core assembly and captured as standalone report pages.

Admin screenshots use the actual configuration template, controller, and translations in a local preview stylesheet.

They show plugin controls, not the surrounding Emby dashboard. Emby's theme and host layout may differ.

Captions identify this distinction. Do not present these captures as Android app or production-server verification.

Build the current core assembly before regenerating the sample report HTML:

```sh
dotnet build src/Emby.LibraryHub.Core -c Release
dotnet run --project tools/docs-samples -- artifacts/docs-samples
```

Use Node.js 18 or newer with Playwright and a matching Chromium installation:

```sh
node tools/docs-screenshots.cjs
```

`PLAYWRIGHT_MODULE` may point to an existing Playwright package. `CHROME_BIN` may select an installed Chromium binary.

`DOCS_SAMPLES` may select the directory containing generated `archive.en.html` and `archive.fr.html`.

The sample generator also accepts `-p:LibraryHubAssemblyPath=/absolute/path/Emby.LibraryHub.Core.dll`.

The capture script creates eight screenshots per language in `docs/assets/screenshots/`.

It checks for unexpected requests, writes, JavaScript errors, and horizontal overflow in the captured member views.

## Review

```sh
mkdocs build --strict
python3 tools/check-docs.py
```

Review both guides in English and French at desktop and phone widths, including the dark documentation theme.

Check that screenshots stay legible, diagrams have no clipped text, and language links keep the current guide.

Click each screenshot and diagram to inspect the original asset at full size.

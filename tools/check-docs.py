#!/usr/bin/env python3
"""Check bilingual page coverage and generated local links after mkdocs build."""
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import unquote, urlsplit, urljoin
import json

root = Path(__file__).resolve().parent.parent
docs = root / 'docs'
site = root / 'site'
errors = []
class Page(HTMLParser):
    def __init__(self, source):
        super().__init__()
        self.language = None
        self.links = []
        self.alternates = {}
        self.feed(source)
    def handle_starttag(self, tag, attributes):
        attrs = dict(attributes)
        if tag == 'html': self.language = attrs.get('lang')
        if tag == 'link' and attrs.get('rel') == 'alternate':
            self.alternates[attrs.get('hreflang')] = attrs.get('href')
        for key in ('src', 'href'):
            if attrs.get(key): self.links.append(attrs[key])

sources = [p for p in docs.glob('*.md') if not p.stem.endswith('.fr')]
for english in sources:
    french = english.with_name(english.stem + '.fr.md')
    if not french.is_file(): errors.append(f'Missing French translation: {english.name}')
    relative = Path('index.html') if english.stem == 'index' else Path(english.stem) / 'index.html'
    for locale, prefix in [('en', Path()), ('fr', Path('fr'))]:
        target = site / prefix / relative
        if not target.is_file():
            errors.append(f'Missing built {locale} page: {relative}')
            continue
        page = Page(target.read_text())
        if page.language != locale: errors.append(f'Wrong HTML language: {target.relative_to(site)}')
        for alternate, alt_prefix in [('en', ''), ('fr', 'fr/')]:
            suffix = '' if english.stem == 'index' else english.stem + '/'
            expected = '/emby-library-hub/' + alt_prefix + suffix
            base_url = 'https://ultreia-io.github.io/emby-library-hub/' + target.relative_to(site).parent.as_posix().rstrip('.') + '/'
            actual = urlsplit(urljoin(base_url, page.alternates.get(alternate, ''))).path
            if actual != expected:
                errors.append(f'Wrong {alternate} alternate: {target.relative_to(site)}')
for html in site.rglob('*.html'):
    for link in Page(html.read_text()).links:
        url = urlsplit(link)
        if url.scheme or url.netloc or not url.path: continue
        path = unquote(url.path)
        if path.startswith('/emby-library-hub/'):
            target = site / path[len('/emby-library-hub/'):]
        elif path.startswith('/'):
            target = site / path.lstrip('/')
        else:
            target = html.parent / path
        if not target.exists(): errors.append(f'Broken link in {html.relative_to(site)}: {link}')
search = json.loads((site / 'search/search_index.json').read_text())
if not any(item.get('location', '').startswith('fr/') for item in search['docs']):
    errors.append('French pages are missing from the search index.')
if errors: raise SystemExit('\n'.join(errors))
print(f'Documentation verified: {len(sources)} pages in each language, matching alternates, local links, bilingual search.')

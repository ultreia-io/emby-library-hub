#!/usr/bin/env python3
"""Validate local/release assets without loading assemblies or using the network."""
import argparse
import hashlib
from pathlib import Path
import re
import xml.etree.ElementTree as ET
import zipfile

root = Path(__file__).resolve().parent.parent
version = ET.parse(root / 'Directory.Build.props').findtext('./PropertyGroup/Version')
if not version or not re.fullmatch(r'(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)', version):
    raise SystemExit('Directory.Build.props must contain a stable x.y.z release version.')
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--tag-only', help='Only verify a v-prefixed release tag and its release notes.')
args = parser.parse_args()
if args.tag_only is not None:
    if args.tag_only != 'v' + version:
        raise SystemExit(f'Tag must match Directory.Build.props: v{version}')
    notes = root / f'docs/release-notes/{version}.md'
    if not notes.is_file() or not notes.read_text().strip():
        raise SystemExit(f'Missing release notes: {notes.name}')
    print(f'Release tag and notes verified: v{version}')
    raise SystemExit(0)

artifacts = root / 'artifacts'
archive_name = f'emby-library-hub-{version}.zip'
expected_assets = {'Emby.LibraryHub.dll', archive_name}
lines = (artifacts / 'SHA256SUMS').read_text().splitlines()
checksums = {}
for line in lines:
    match = re.fullmatch(r'([0-9a-f]{64})  ([^/\\]+)', line)
    if not match or match[2] in checksums:
        raise SystemExit('Malformed or duplicate checksum entry.')
    checksums[match[2]] = match[1]
if set(checksums) != expected_assets:
    raise SystemExit('SHA256SUMS must list exactly the DLL and versioned ZIP.')
for name, checksum in checksums.items():
    if hashlib.sha256((artifacts / name).read_bytes()).hexdigest() != checksum:
        raise SystemExit(f'Checksum mismatch: {name}')
expected_files = {'Emby.LibraryHub.dll', 'INSTALL.md', 'INSTALL.fr.md', 'THIRD-PARTY-NOTICES.txt', 'LICENSE', 'NOTICE'}
with zipfile.ZipFile(artifacts / archive_name) as archive:
    if set(archive.namelist()) != expected_files or len(archive.namelist()) != len(expected_files):
        raise SystemExit('ZIP must contain only the plugin DLL, installation guide, license, and notices.')
    if archive.testzip() is not None:
        raise SystemExit('ZIP integrity check failed.')
    if archive.read('Emby.LibraryHub.dll') != (artifacts / 'Emby.LibraryHub.dll').read_bytes():
        raise SystemExit('Packaged and standalone DLLs differ.')
    for name, source in [('INSTALL.md', 'docs/INSTALL.md'), ('INSTALL.fr.md', 'docs/INSTALL.fr.md'), ('THIRD-PARTY-NOTICES.txt', 'docs/THIRD-PARTY-NOTICES.txt'), ('LICENSE', 'LICENSE'), ('NOTICE', 'NOTICE')]:
        if archive.read(name) != (root / source).read_bytes():
            raise SystemExit(f'Stale packaged document: {name}')
print(f'Package verified: {archive_name} (single DLL, licenses, guide, SHA-256)')

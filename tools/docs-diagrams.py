#!/usr/bin/env python3
"""Render the bilingual administration diagrams as self-contained, accessible SVGs."""
from html import escape
from pathlib import Path

DEST = Path(__file__).resolve().parents[1] / 'docs/assets/diagrams'
DEST.mkdir(parents=True, exist_ok=True)


def render(name, language, title, subtitle, cards, arrows, note, height=650):
    """Draw fixed-layout cards and connectors, keeping labels independent of the site theme."""
    parts = [f'''<svg xmlns="http://www.w3.org/2000/svg" width="1120" height="{height}" viewBox="0 0 1120 {height}" role="img" aria-labelledby="title description" lang="{language}">
<title id="title">{escape(title)}</title><desc id="description">{escape(subtitle + ' ' + note)}</desc>
<defs><marker id="arrow" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto"><path d="M0 0L8 4L0 8" fill="#5a8b71"/></marker></defs>
<rect width="1120" height="{height}" rx="24" fill="#f1f7f3"/>
<g font-family="DejaVu Sans,Arial,sans-serif"><text x="36" y="38" fill="#1b6d48" font-size="12" font-weight="700" letter-spacing="2">LIBRARY HUB / ADMINISTRATION</text>
<text x="36" y="79" fill="#173e2d" font-size="30" font-weight="700">{escape(title)}</text>
<text x="36" y="111" fill="#52695b" font-size="17">{escape(subtitle)}</text>''']
    for d in arrows:
        parts.append(f'<path d="{d}" fill="none" stroke="#5a8b71" stroke-width="2.5" marker-end="url(#arrow)"/>')
    for x, y, w, h, badge, heading, lines, accent in cards:
        color = '#9b5a26' if accent else '#1b6d48'
        parts.append(f'<rect x="{x}" y="{y+3}" width="{w}" height="{h}" rx="16" fill="#163d2c" opacity=".06"/>')
        parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="16" fill="white" stroke="{color if accent else "#d4e3d9"}"/>')
        parts.append(f'<rect x="{x+20}" y="{y+18}" width="34" height="26" rx="8" fill="{color}"/>')
        parts.append(f'<text x="{x+37}" y="{y+36}" text-anchor="middle" font-size="13" font-weight="700" fill="white">{escape(badge)}</text>')
        parts.append(f'<text x="{x+20}" y="{y+75}" font-size="21" font-weight="700" fill="#173e2d">{escape(heading)}</text>')
        for n, line in enumerate(lines):
            parts.append(f'<text x="{x+20}" y="{y+105+n*25}" font-size="16" fill="#52695b">{escape(line)}</text>')
    parts.append(f'<rect x="36" y="{height-69}" width="1048" height="42" rx="10" fill="#e2eee6"/>')
    parts.append(f'<text x="55" y="{height-42}" font-size="16" font-weight="600" fill="#224d36">{escape(note)}</text></g></svg>')
    (DEST / f'{name}.{language}.svg').write_text('\n'.join(parts) + '\n')


for lang in ('en', 'fr'):
    fr = lang == 'fr'
    render('access', lang,
           'Un compte, trois règles d’accès' if fr else 'One account, three access rules',
           'Le serveur authentifie chaque membre ; chaque espace applique sa propre règle.' if fr else
           'The server authenticates every member. Each area applies its own access rule.',
           [(36, 240, 258, 190, 'ID', 'Compte Emby' if fr else 'Emby account',
             ['Connexion au serveur', 'Compte actif requis', 'Aucun compte distinct'] if fr else
             ['Sign in to your server', 'Active account required', 'No separate account'], False),
            (375, 146, 709, 125, '01', 'Explorer et Rechercher' if fr else 'Browse and Search',
             ['Médiathèques proposées ∩ droits Emby du membre'] if fr else
             ['Administrator-offered libraries ∩ the member’s Emby access'], False),
            (375, 295, 709, 125, '02', 'Archives communes' if fr else 'Shared archive',
             ['Tous les rapports publiés, pour tous les membres connectés'] if fr else
             ['All published reports, visible to every signed-in member'], True),
            (375, 444, 709, 125, '03', 'Courriels personnels' if fr else 'Personal emails',
             ['Abonnement confirmé + droits revérifiés avant chaque envoi'] if fr else
             ['Confirmed subscription + permissions rechecked before each send'], False)],
           ['M294 334H334V209H365', 'M334 334V357H365', 'M334 357V506H365'],
           'Réglages, sauvegardes et liste des abonnés : administrateurs uniquement.' if fr else
           'Settings, backups, and the subscriber list are administrator-only.', height=680)
    render('delivery', lang,
           'Deux sorties, un historique commun' if fr else 'Two outputs, one shared history',
           'Les archives et les courriels ont des cycles indépendants.' if fr else
           'Saved archives and subscriber emails have independent lifecycles.',
           [(36, 147, 310, 171, '01', 'Changements Emby' if fr else 'Emby changes',
             ['Ajouts et suppressions', 'Événements + inventaire'] if fr else
             ['Additions and removals', 'Events + reconciliation'], False),
            (405, 147, 310, 171, '02', 'Historique enregistré' if fr else 'Saved history',
             ['Journées dans le fuseau', 'configuré par le serveur'] if fr else
             ['Calendar days in the', 'configured time zone'], False),
            (774, 147, 310, 171, '03', 'Tâche planifiée' if fr else 'Scheduled task',
             ['Toutes les cinq minutes', 'Heure d’envoi enregistrée'] if fr else
             ['Every five minutes', 'Saved delivery time'], False),
            (36, 378, 310, 196, 'A', 'Archives HTML' if fr else 'HTML archive',
             ['Pages publiées et conservées', 'Communes aux membres', 'La lecture n’envoie rien'] if fr else
             ['Published, saved pages', 'Shared with all members', 'Viewing never sends email'], True),
            (405, 378, 310, 196, 'B', 'Vérifications' if fr else 'Recipient checks',
             ['Compte actif et confirmé', 'Droits actuels + jour terminé', 'Jour vide : aucun courriel'] if fr else
             ['Active, confirmed account', 'Current access + finished day', 'Empty day: no email'], False),
            (774, 378, 310, 196, '→', 'Envoi SMTP' if fr else 'SMTP delivery',
             ['Au plus un rapport par jour', 'Retard : le plus ancien d’abord', 'Échec : nouvel essai après 1 h'] if fr else
             ['At most one report per day', 'Backlog: oldest day first', 'Failure: retry after one hour'], False)],
           ['M346 230H395', 'M715 230H764', 'M560 318V332H191V368',
            'M929 318V350H560V368', 'M715 465H764'],
           'Générer ou réinitialiser les archives ne renvoie aucun courriel.' if fr else
           'Generating or resetting archives never resends subscriber emails.', height=674)
    render('maintenance', lang,
           'Une mise à jour qui conserve les données' if fr else 'An update that preserves your data',
           'La DLL, la configuration et les données forment votre point de retour.' if fr else
           'The DLL, configuration, and data together form your recovery point.',
           [(36, 154, 240, 211, '01', 'Arrêter Emby' if fr else 'Stop Emby',
             ['Prévoir une interruption', 'des lectures en cours'] if fr else
             ['Allow for interruption', 'of active playback'], False),
            (305, 154, 240, 211, '02', 'Sauvegarder' if fr else 'Back up',
             ['DLL + configuration', 'Tout data/library-hub/', 'Copie privée cohérente'] if fr else
             ['DLL + configuration', 'All of data/library-hub/', 'One private snapshot'], False),
            (574, 154, 240, 211, '03', 'Remplacer la DLL' if fr else 'Replace the DLL',
             ['Conserver les données', 'Ancienne DLL hors du', 'dossier des plugins'] if fr else
             ['Keep existing data', 'Store the old DLL outside', 'the plugin directory'], False),
            (843, 154, 241, 211, '04', 'Redémarrer' if fr else 'Restart & check',
             ['Rapport connu', 'Accès d’un membre', 'État des abonnements'] if fr else
             ['Open a known report', 'Check a member’s access', 'Check subscription status'], False)],
           ['M276 246H295', 'M545 246H564', 'M814 246H833'],
           'Exporter les réglages en JSON ne remplace pas une sauvegarde complète.' if fr else
           'A settings JSON export does not replace a complete backup.', height=470)
print(f'Generated six diagrams in {DEST}')

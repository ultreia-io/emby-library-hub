define([], function () {
  'use strict';
  var messages = {
  "SearchLibraries": {"en": "Search libraries", "fr": "Rechercher dans les médiathèques"},
  "BrowseCatalog": {"en": "Catalog browsing", "fr": "Exploration du catalogue"},
  "BrowseLibrariesHelp": {"en": "Choose the libraries offered in Library Hub. Members can select a subset of these, subject to their Emby permissions. This does not affect archives or emails.", "fr": "Choisissez les médiathèques proposées dans Library Hub. Chaque membre peut en sélectionner une partie, selon ses droits Emby. Ce choix ne modifie ni les archives ni les courriels."},
  "NoBrowseLibraries": {"en": "No libraries are configured in Emby.", "fr": "Aucune médiathèque n’est configurée dans Emby."},
  "BrowseLibrariesFailed": {"en": "Could not load libraries. Reload this page to change the selection.", "fr": "Impossible de charger les médiathèques. Rechargez cette page pour modifier la sélection."},
  "DeliverySettings": {"en": "Daily emails", "fr": "Courriels quotidiens"},
  "BrowseLibraries": {"en": "Browse libraries", "fr": "Explorer les médiathèques"},
  "PreparingPreview": {
    "en": "Preparing today’s preview…",
    "fr": "Préparation de l’aperçu du jour…"
  },
  "GeneratingReports": {
    "en": "Generating the selected daily reports…",
    "fr": "Génération des rapports quotidiens sélectionnés…"
  },
  "LoadingArchive": {
    "en": "Loading your archive…",
    "fr": "Chargement de vos archives…"
  },
  "Intro": {
    "en": "Browse your libraries, read daily archives, and subscribe to updates in French or English.",
    "fr": "Explorez vos médiathèques, consultez les archives et abonnez-vous aux nouveautés en français ou en anglais."
  },
  "DeliveryEnabled": {
    "en": "Enable daily subscriber emails",
    "fr": "Activer les courriels quotidiens aux abonnés"
  },
  "DeliveryHelp": {
    "en": "Tracking continues while delivery is disabled. Only confirmed mailing-list subscribers receive reports.",
    "fr": "Le suivi continue lorsque l’envoi est désactivé. Seuls les abonnés ayant confirmé leur inscription reçoivent les rapports."
  },
  "Language": {
    "en": "Report and preview language",
    "fr": "Langue des rapports et de l’aperçu"
  },
  "LanguageHelp": {
    "en": "Subscribers choose their own email language. Library names and titles remain as stored in Emby.",
    "fr": "Les abonnés choisissent la langue de leurs courriels. Les noms des médiathèques et les titres restent ceux d’Emby."
  },
  "DeliveryTime": {
    "en": "Delivery time",
    "fr": "Heure d’envoi"
  },
  "DeliveryTimeHelp": {
    "en": "The previous day's changes, within about five minutes of this time.",
    "fr": "Les changements de la veille, dans les cinq minutes environ suivant cette heure."
  },
  "TimeZoneId": {
    "en": "Time zone",
    "fr": "Fuseau horaire"
  },
  "TimeZoneHelp": {
    "en": "For example Europe/Paris or Etc/UTC.",
    "fr": "Par exemple Europe/Paris ou Etc/UTC."
  },
  "VideoLinks": {
    "en": "Include links to added videos",
    "fr": "Inclure les liens vers les vidéos ajoutées"
  },
  "PublicServerUrl": {
    "en": "Public Emby server URL",
    "fr": "Adresse publique du serveur Emby"
  },
  "ServerUrlHelp": {
    "en": "The server's base address. Recipients sign in with their own Emby accounts.",
    "fr": "L’adresse de base du serveur. Les destinataires se connectent avec leur propre compte Emby."
  },
  "Sender": {
    "en": "Sender email",
    "fr": "Adresse de l’expéditeur"
  },
  "SmtpHost": {
    "en": "SMTP server",
    "fr": "Serveur SMTP"
  },
  "SmtpPort": {
    "en": "SMTP port",
    "fr": "Port SMTP"
  },
  "StartTls": {
    "en": "Use STARTTLS on ports other than 465",
    "fr": "Utiliser STARTTLS sur les ports autres que 465"
  },
  "StartTlsHelp": {
    "en": "Port 465 always uses TLS from connection start. Other ports use STARTTLS when checked (usually 587).",
    "fr": "Le port 465 utilise toujours TLS dès la connexion. Si cette case est cochée, les autres ports utilisent STARTTLS (généralement 587)."
  },
  "SmtpUsername": {
    "en": "SMTP username",
    "fr": "Identifiant SMTP"
  },
  "SmtpPassword": {
    "en": "SMTP password",
    "fr": "Mot de passe SMTP"
  },
  "PasswordHelp": {
    "en": "Stored in Emby's plugin configuration. Restrict access to its data directory.",
    "fr": "Enregistré dans la configuration du plugin Emby. Limitez l’accès à son dossier de données."
  },
  "Save": {
    "en": "Save",
    "fr": "Enregistrer"
  },
  "Backup": {
    "en": "Configuration backup",
    "fr": "Sauvegarde de la configuration"
  },
  "BackupHelp": {
    "en": "Export saved settings as JSON. Restore a backup to replace and save all settings in one operation.",
    "fr": "Exportez les paramètres enregistrés au format JSON. Restaurez une sauvegarde pour remplacer et enregistrer tous les paramètres en une seule opération."
  },
  "BackupPrivacy": {
    "en": "The backup includes your SMTP password. Keep it private. Plugin updates normally retain your settings.",
    "fr": "La sauvegarde contient votre mot de passe SMTP. Gardez-la privée. Les mises à jour du plugin conservent normalement vos paramètres."
  },
  "Export": {
    "en": "Export JSON",
    "fr": "Exporter en JSON"
  },
  "Restore": {
    "en": "Restore from JSON",
    "fr": "Restaurer depuis un fichier JSON"
  },
  "PublicReports": {
    "en": "Private community reports",
    "fr": "Rapports de la communauté privée"
  },
  "PublicReportsHelp": {
    "en": "Daily reports are prepared automatically. Reading them requires an active Emby account.",
    "fr": "Les rapports quotidiens sont préparés automatiquement. Leur consultation exige un compte Emby actif."
  },
  "PublicReportsPrivacy": {
    "en": "All signed-in members read the same saved archive. Emails follow each subscriber’s library permissions.",
    "fr": "Tous les membres connectés lisent les mêmes archives enregistrées. Les courriels respectent les droits du destinataire."
  },
  "OpenArchive": {
    "en": "Open private archive",
    "fr": "Ouvrir les archives privées"
  },
  "ShareArchive": {
    "en": "Archive link (Emby sign-in required)",
    "fr": "Lien des archives (connexion Emby obligatoire)"
  },
  "ArchiveAccess": {
    "en": "Members open this page through the Emby web user menu. Android TV users can sign in on a phone or computer.",
    "fr": "Les membres ouvrent cette page depuis le menu Emby Web. Les utilisateurs d’Android TV se connectent sur téléphone ou ordinateur."
  },
  "ResetArchive": {
    "en": "Reset archive list",
    "fr": "Réinitialiser la liste des archives"
  },
  "ResetHelp": {
    "en": "A backup is kept. This clears published reports, keeping settings, subscribers, history, and the archive link.",
    "fr": "Une sauvegarde est conservée. Les rapports publiés sont retirés de la liste ; les paramètres, abonnés, historiques et liens sont conservés."
  },
  "ResetNext": {
    "en": "Then choose dates below and Generate HTML only. Automatic publication resumes with today and future days.",
    "fr": "Choisissez ensuite les dates ci-dessous et cliquez sur « Générer le HTML uniquement ». La publication automatique reprend à partir d’aujourd’hui."
  },
  "Subscriptions": {
    "en": "Email subscriptions",
    "fr": "Abonnements par courriel"
  },
  "SubscriptionsHelp": {
    "en": "Only signed-in Emby users can request and confirm subscriptions.",
    "fr": "Seuls les utilisateurs Emby connectés peuvent demander et confirmer un abonnement."
  },
  "SubscriptionsDelivery": {
    "en": "Confirmed subscribers receive separate emails at the saved delivery time when email delivery is enabled.",
    "fr": "Lorsque l’envoi est activé, chaque abonné confirmé reçoit un courriel individuel à l’heure enregistrée."
  },
  "SubscriptionsEnabled": {
    "en": "Enable community subscriptions and subscriber delivery",
    "fr": "Activer les inscriptions de la communauté et l’envoi aux abonnés"
  },
  "SubscriptionsPause": {
    "en": "Turning this off pauses subscriber emails and new subscriptions; unsubscribe remains available.",
    "fr": "Désactiver cette option suspend les courriels aux abonnés et les nouvelles inscriptions. Le désabonnement reste disponible."
  },
  "SaveSettings": {
    "en": "Save settings",
    "fr": "Enregistrer les paramètres"
  },
  "ShareSubscription": {
    "en": "Community subscription link (Emby sign-in required)",
    "fr": "Lien d’inscription de la communauté (connexion Emby obligatoire)"
  },
  "OpenSubscription": {
    "en": "Open community subscription page",
    "fr": "Ouvrir la page d’inscription privée"
  },
  "RefreshSubscribers": {
    "en": "Refresh subscribers",
    "fr": "Actualiser les abonnés"
  },
  "Email": {
    "en": "Email",
    "fr": "Adresse électronique"
  },
  "SubscriberLanguage": {
    "en": "Language",
    "fr": "Langue"
  },
  "Status": {
    "en": "Status",
    "fr": "État"
  },
  "Action": {
    "en": "Action",
    "fr": "Action"
  },
  "GenerateReports": {
    "en": "Generate HTML reports",
    "fr": "Générer les rapports HTML"
  },
  "GenerateHelp": {
    "en": "Generate or regenerate the archive for a date range. This never sends emails or changes subscriber delivery.",
    "fr": "Générez ou régénérez les archives pour une période. Cette opération n’envoie aucun courriel et ne modifie pas les envois aux abonnés."
  },
  "DateRangeHelp": {
    "en": "Both dates are included, from 00:00 to 23:59 in your saved time zone. Today includes changes so far.",
    "fr": "Les deux dates sont incluses, de 00:00 à 23:59 dans le fuseau enregistré. Aujourd’hui comprend les changements déjà observés."
  },
  "HistoryHelp": {
    "en": "Past additions use Emby's recorded dates. Past removals are available only in the plugin's retained history.",
    "fr": "Les ajouts passés utilisent les dates enregistrées par Emby. Les suppressions passées sont disponibles uniquement dans l’historique conservé par le plugin."
  },
  "From": {
    "en": "From",
    "fr": "Du"
  },
  "To": {
    "en": "To",
    "fr": "Au"
  },
  "Calendar": {
    "en": "Calendar",
    "fr": "Calendrier"
  },
  "DateFormat": {
    "en": "YYYY/MM/DD — year / month / day",
    "fr": "AAAA/MM/JJ — année / mois / jour"
  },
  "Generate": {
    "en": "Generate HTML only",
    "fr": "Générer le HTML uniquement"
  },
  "OnceDaily": {
    "en": "Subscribers receive at most one daily report per calendar day, after the saved delivery time. Empty days are skipped.",
    "fr": "Les abonnés reçoivent au maximum un rapport par jour civil, après l’heure enregistrée. Les jours sans changement sont ignorés."
  },
  "SaveBeforeGenerate": {
    "en": "Save settings changes before generating reports. Rebuilding or resetting the archive never resends subscriber emails.",
    "fr": "Enregistrez les paramètres avant de générer les rapports. Reconstruire ou réinitialiser les archives ne renvoie jamais de courriels aux abonnés."
  },
  "Preview": {
    "en": "Preview",
    "fr": "Aperçu"
  },
  "PreviewHelp": {
    "en": "Save settings first. Preview refreshes tracked changes without sending email or clearing them.",
    "fr": "Enregistrez d’abord les paramètres. L’aperçu actualise les changements suivis sans envoyer de courriel ni les effacer."
  },
  "CalendarDayHelp": {
    "en": "Each report covers a complete calendar day (00:00–23:59) in the selected time zone, sent the following day.",
    "fr": "Chaque rapport couvre une journée civile complète (00:00–23:59) dans le fuseau choisi et est envoyé le lendemain."
  },
  "PreviewToday": {
    "en": "Preview today’s report",
    "fr": "Aperçu du rapport d’aujourd’hui"
  },
  "FromAria": {
    "en": "From date, YYYY/MM/DD",
    "fr": "Date de début, AAAA/MM/JJ"
  },
  "ToAria": {
    "en": "To date, YYYY/MM/DD",
    "fr": "Date de fin, AAAA/MM/JJ"
  },
  "ChooseFrom": {
    "en": "Choose from date",
    "fr": "Choisir la date de début"
  },
  "ChooseTo": {
    "en": "Choose to date",
    "fr": "Choisir la date de fin"
  },
  "DatePlaceholder": {
    "en": "YYYY/MM/DD",
    "fr": "AAAA/MM/JJ"
  },
  "OperationFailed": {
    "en": "Operation failed. Check the settings and Emby server log.",
    "fr": "L’opération a échoué. Vérifiez les paramètres et le journal du serveur Emby."
  },
  "InvalidJson": {
    "en": "This file is not valid JSON. Your settings were not changed.",
    "fr": "Ce fichier JSON n’est pas valide. Vos paramètres n’ont pas été modifiés."
  },
  "UnsupportedBackup": {
    "en": "This is not a supported Library Hub configuration backup.",
    "fr": "Ce fichier n’est pas une sauvegarde de configuration Library Hub prise en charge."
  },
  "InvalidSetting": {
    "en": "The backup has a missing or invalid setting: {0}.",
    "fr": "Un paramètre de la sauvegarde est absent ou invalide : {0}."
  },
  "InvalidBackupValues": {
    "en": "The backup contains an invalid language, delivery time, or SMTP port.",
    "fr": "La sauvegarde contient une langue, une heure d’envoi ou un port SMTP invalide."
  },
  "CalendarFailed": {
    "en": "Cannot read the server date. Refresh to try again.",
    "fr": "Impossible de lire la date du serveur. Actualisez la page pour réessayer."
  },
  "ArchiveUnavailable": {
    "en": "Archive is not available yet.",
    "fr": "Les archives ne sont pas encore disponibles."
  },
  "ArchiveReset": {
    "en": "{0} reports removed from the archive list. A backup was kept. Choose dates and Generate HTML only to rebuild.",
    "fr": "{0} rapports retirés de la liste des archives. Une sauvegarde a été conservée. Choisissez les dates et cliquez sur « Générer le HTML uniquement » pour les reconstruire."
  },
  "ArchiveResetFailed": {
    "en": "Archive reset failed. Check the server log before trying again.",
    "fr": "La réinitialisation des archives a échoué. Consultez le journal du serveur avant de réessayer."
  },
  "InvalidDates": {
    "en": "Choose valid From and To dates (YYYY/MM/DD), in order and no later than today.",
    "fr": "Choisissez des dates de début et de fin valides (AAAA/MM/JJ), dans l’ordre et au plus tard aujourd’hui."
  },
  "ReportsGenerated": {
    "en": "{0} HTML reports published. No emails were sent.",
    "fr": "{0} rapports HTML publiés. Aucun courriel n’a été envoyé."
  },
  "GenerationFailed": {
    "en": "Report generation failed. Check the server log and try again.",
    "fr": "La génération des rapports a échoué. Consultez le journal du serveur et réessayez."
  },
  "Unsubscribe": {
    "en": "Unsubscribe",
    "fr": "Désabonner"
  },
  "RemoveFailed": {
    "en": "Unable to remove this subscriber. Refresh and try again.",
    "fr": "Impossible de désabonner cette adresse. Actualisez la page et réessayez."
  },
  "SubscriberCount": {
    "en": "{0} subscribed; {1} pending, expired, or requiring account confirmation.",
    "fr": "Abonnés confirmés : {0} ; en attente, expirés ou sans compte confirmé : {1}."
  },
  "SubscribersFailed": {
    "en": "Unable to load subscribers. Refresh to try again.",
    "fr": "Impossible de charger les abonnés. Actualisez la page pour réessayer."
  },
  "SettingsSaved": {
    "en": "Settings saved. They apply to the next run; no restart is needed.",
    "fr": "Paramètres enregistrés. Ils s’appliqueront à la prochaine exécution ; aucun redémarrage n’est nécessaire."
  },
  "Exported": {
    "en": "Configuration exported. This file contains your SMTP password; keep it private.",
    "fr": "Configuration exportée. Ce fichier contient votre mot de passe SMTP ; gardez-le privé."
  },
  "BackupTooLarge": {
    "en": "The backup exceeds 64 KB. Your settings were not changed.",
    "fr": "La sauvegarde dépasse 64 Ko. Vos paramètres n’ont pas été modifiés."
  },
  "Restored": {
    "en": "Configuration restored and saved. No restart is needed.",
    "fr": "Configuration restaurée et enregistrée. Aucun redémarrage n’est nécessaire."
  },
  "Subject": {
    "en": "Subject: {0}",
    "fr": "Objet : {0}"
  },
  "Subscribed": {
    "en": "Subscribed",
    "fr": "Abonné"
  },
  "Expired": {
    "en": "Expired",
    "fr": "Expiré"
  },
  "Awaiting confirmation": {
    "en": "Awaiting confirmation",
    "fr": "En attente de confirmation"
  },
  "Delivery failed; retry scheduled.": {
    "en": "Delivery failed; retry scheduled.",
    "fr": "Échec de l’envoi ; une nouvelle tentative est prévue."
  },
  "Choose both From and To dates.": {
    "en": "Choose both From and To dates.",
    "fr": "Choisissez les dates de début et de fin."
  },
  "From must be on or before To.": {
    "en": "From must be on or before To.",
    "fr": "La date de début doit précéder la date de fin ou être identique."
  },
  "The date range cannot include future days.": {
    "en": "The date range cannot include future days.",
    "fr": "La période ne peut pas inclure de dates futures."
  },
  "The report archive is not configured.": {
    "en": "The report archive is not configured.",
    "fr": "Les archives de rapports ne sont pas configurées."
  },
  "Emby is starting or scanning. Try generation again when it finishes.": {
    "en": "Emby is starting or scanning. Try generation again when it finishes.",
    "fr": "Emby démarre ou analyse les médiathèques. Relancez la génération lorsque cette opération sera terminée."
  },
  "Invalid report date.": {
    "en": "Invalid report date.",
    "fr": "La date du rapport n’est pas valide."
  },
  "Cannot publish a future report.": {
    "en": "Cannot publish a future report.",
    "fr": "Impossible de publier un rapport pour une date future."
  },
  "OpenArchiveArrow": {
    "en": "Open private archive ↗",
    "fr": "Ouvrir les archives privées ↗"
  },
  "LibraryUpdates": {
    "en": "Library updates",
    "fr": "Nouveautés des médiathèques"
  },
  "ReportsUnavailable": {
    "en": "Reports are not available yet. Please try again shortly.",
    "fr": "Les rapports ne sont pas encore disponibles. Réessayez dans quelques instants."
  },
  "CommunityPrivacy": {
    "en": "Private community: sign in to read the shared, saved reports.",
    "fr": "Communauté privée : connectez-vous pour consulter les rapports enregistrés, communs à tous les membres."
  },
  "MySubscription": {
    "en": "My email subscription",
    "fr": "Mon abonnement par courriel"
  },
  "RequestSubscription": {
    "en": "Request confirmation email",
    "fr": "Demander le courriel de confirmation"
  },
  "ConfirmSubscription": {
    "en": "Confirm my subscription",
    "fr": "Confirmer mon abonnement"
  },
  "ConfirmUnsubscribe": {
    "en": "Confirm unsubscribe",
    "fr": "Confirmer mon désabonnement"
  },
  "EnrollmentPaused": {
    "en": "New subscriptions and deliveries are paused. You can still unsubscribe.",
    "fr": "Les inscriptions et envois sont suspendus. Vous pouvez toujours vous désabonner."
  },
  "PrivateReportsUnavailable": {
    "en": "This report is unavailable or outside your current Emby access. Sign in and try again.",
    "fr": "Ce rapport est indisponible ou hors de vos droits Emby actuels. Connectez-vous et réessayez."
  },
  "SignInRequired": {
    "en": "Sign in with your own active Emby account to manage subscriptions.",
    "fr": "Connectez-vous avec votre propre compte Emby actif pour gérer vos abonnements."
  },
  "InvalidMemberLink": {
    "en": "Invalid or expired link, or a different Emby account. Sign in with the account that requested it.",
    "fr": "Lien invalide ou expiré, ou autre compte Emby. Connectez-vous avec le compte à l’origine de la demande."
  },
  "ConfirmationRequested": {
    "en": "If the request can be processed, a confirmation email will arrive. Open it while signed in to this Emby account.",
    "fr": "Si la demande peut être traitée, vous recevrez un courriel. Ouvrez son lien avec ce même compte Emby connecté."
  },
  "MemberConfirmed": {
    "en": "Subscription confirmed. Future reports will respect this Emby account’s access.",
    "fr": "Abonnement confirmé. Les prochains rapports respecteront les droits de ce compte Emby."
  },
  "MemberRemoved": {
    "en": "You are unsubscribed. No further reports will be sent.",
    "fr": "Vous êtes désabonné. Aucun autre rapport ne vous sera envoyé."
  },
  "Needs account confirmation": {
    "en": "Needs Emby account confirmation",
    "fr": "Confirmation avec un compte Emby requise"
  }
};

  function create(page) {
    var language = 'en';
    function fallbackLocale() {
      var root = typeof document !== 'undefined' && document.documentElement;
      return (root && (root.getAttribute('data-culture') || root.lang)) ||
        (typeof navigator !== 'undefined' && navigator.language) || 'en';
    }
    function text(key) {
      var message = messages[key];
      var value = message ? message[language] || message.en : key;
      var args = arguments;
      return value.replace(/\{(\d+)\}/g, function (_, index) { return String(args[Number(index) + 1]); });
    }
    function apply(value) {
      language = /^fr(?:[-_]|$)/i.test(value || '') ? 'fr' : 'en';
      page.setAttribute('lang', language);
      [['data-digest-text', null], ['data-digest-aria', 'aria-label'],
        ['data-digest-placeholder', 'placeholder'], ['data-digest-title', 'title']].forEach(function (pair) {
        page.querySelectorAll('[' + pair[0] + ']').forEach(function (element) {
          var translated = text(element.getAttribute(pair[0]));
          if (pair[1]) element.setAttribute(pair[1], translated);
          else element.textContent = translated;
        });
      });
    }
    apply(fallbackLocale());
    return {
      text: text,
      error: function (message) { return messages[message] ? text(message) : text('GenerationFailed'); },
      load: function () {
        // Emby 4.11 exposes its active user-interface locale through this module.
        // It includes the user's display preference and Emby's server/browser fallback.
        return Promise.resolve().then(function () {
          if (typeof Emby === 'undefined' || typeof Emby.importModule !== 'function') return null;
          return Emby.importModule('./modules/common/globalize.js');
        }).then(function (globalize) {
          globalize = globalize && (globalize.default || globalize);
          apply(globalize && globalize.getCurrentLocale ? globalize.getCurrentLocale() : fallbackLocale());
        }).catch(function () { apply(fallbackLocale()); });
      }
    };
  }
  return {create: create};
});

using System;
using System.Collections.Generic;
using System.IO;
using Emby.LibraryHub.Core;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Serialization;

namespace Emby.LibraryHub;

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public DigestOptions Digest { get; set; } = new DigestOptions();
}

public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
{
    public static Plugin Instance { get; private set; } = null!;
    public const string PluginId = "f6975142-a690-4cdc-b98b-2c43b2d084ed";
    public Plugin(IApplicationPaths paths, IXmlSerializer serializer) : base(paths, serializer)
    {
        Instance = this;
        StatePath = Path.Combine(paths.DataPath, "library-hub", "state.json");
    }

    public ImageFormat ThumbImageFormat => ImageFormat.Png;
    public Stream GetThumbImage() => GetType().Assembly.GetManifestResourceStream("Emby.LibraryHub.Standalone.logo.png")!;

    public string StatePath { get; }
    public override string Name => "Library Hub";
    public override string Description => "Browse your libraries, read daily archives, and subscribe to updates in French or English.";
    public override Guid Id => new Guid(PluginId);

    public override void UpdateConfiguration(BasePluginConfiguration configuration)
    {
        var options = ((PluginConfiguration)configuration).Digest;
        options.Validate();
        if (options.DeliveryEnabled || options.SubscriptionsEnabled) SmtpDigestSender.ValidateTransport(options);
        options.Recipient = ""; // Ignore the retired single-recipient setting in old JSON backups.
        base.UpdateConfiguration(configuration);
    }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = "libraryhubbrowse", DisplayName = Configuration.Digest.Language == "fr" ? "Explorer / Library Hub" : "Browse / Library Hub",
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.browse.html", EnableInUserMenu = true, MenuIcon = "search"
        };
        yield return new PluginPageInfo
        {
            Name = "libraryhubbrowsejs", EmbeddedResourcePath = GetType().Namespace + ".Configuration.browse.js"
        };
        yield return new PluginPageInfo
        {
            Name = "libraryhub",
            DisplayName = "Library Hub",
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html",
            IsMainConfigPage = true,
            EnableInMainMenu = true,
            // Unsectioned plugin pages are appended after Emby's built-in Advanced items.
            MenuIcon = "mail"
        };
        yield return new PluginPageInfo
        {
            Name = "libraryhubreports",
            DisplayName = Configuration.Digest.Language == "fr" ? "Nouveautés" : "Library updates",
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.reports.html",
            EnableInUserMenu = true,
            IsMainConfigPage = false,
            MenuIcon = "article"
        };
        yield return new PluginPageInfo
        {
            Name = "libraryhubsubscriptions",
            DisplayName = Configuration.Digest.Language == "fr" ? "Mon abonnement" : "My subscription",
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.subscriptions.html",
            IsMainConfigPage = false
        };
        yield return new PluginPageInfo
        {
            Name = "libraryhubsubscriptionsjs",
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.subscriptions.js",
            IsMainConfigPage = false
        };
        yield return new PluginPageInfo
        {
            Name = "libraryhubreportsjs",
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.reports.js",
            IsMainConfigPage = false
        };
        yield return new PluginPageInfo
        {
            Name = "libraryhublocalejs",
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.locale.js",
            IsMainConfigPage = false
        };
        yield return new PluginPageInfo
        {
            Name = "libraryhubjs",
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.js",
            IsMainConfigPage = false
        };
    }
}

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Emby.LibraryHub;

internal static class BundledDependencies
{
    // Emby loads plugins in isolated contexts, where adjacent DLLs are not resolved.
    // Load embedded dependencies into this plugin's context before any SMTP code runs.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255",
        Justification = "Emby dependency loading must be initialized before plugin code is JIT compiled.")]
    [ModuleInitializer]
    internal static void Initialize()
    {
        var assembly = typeof(BundledDependencies).Assembly;
        var context = AssemblyLoadContext.GetLoadContext(assembly)
            ?? throw new InvalidOperationException("The plugin has no assembly load context.");
        Load(context, assembly, "MimeKitLite");
        Load(context, assembly, "MailKitLite");
    }

    private static void Load(AssemblyLoadContext context, Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream("Emby.LibraryHub.Dependencies." + name + ".dll")
            ?? throw new FileNotFoundException("Missing bundled dependency: " + name);
        context.LoadFromStream(stream);
    }
}

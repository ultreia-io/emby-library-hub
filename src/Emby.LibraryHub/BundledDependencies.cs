using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Emby.LibraryHub;

/// <summary>Resolves private embedded dependencies on both .NET and Mono without taking over other plugins' loads.</summary>
internal static class BundledDependencies
{
    private const string Prefix = "Emby.LibraryHub.Dependencies.";
    private static readonly Assembly Owner = typeof(BundledDependencies).Assembly;
    private static readonly Dictionary<string, Assembly> Loaded = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object Gate = new();

    /// <summary>Registers the resolver before plugin code can request the mail assemblies.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255",
        Justification = "Emby dependency loading must be initialized before plugin code is JIT compiled.")]
    [ModuleInitializer]
    internal static void Initialize()
    {
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
    }

    /// <summary>Loads a known embedded assembly only for this plugin or one of its own dependencies.</summary>
    private static Assembly? Resolve(object? sender, ResolveEventArgs args)
    {
        lock (Gate)
        {
            if (args.RequestingAssembly != Owner && !Loaded.ContainsValue(args.RequestingAssembly!)) return null;
            var name = new AssemblyName(args.Name).Name;
            if (name == null) return null;
            if (Loaded.TryGetValue(name, out var loaded)) return loaded;
            using var stream = Owner.GetManifestResourceStream(Prefix + name + ".dll");
            if (stream == null) return null;
            using var bytes = new MemoryStream();
            stream.CopyTo(bytes);
            loaded = Assembly.Load(bytes.ToArray());
            Loaded.Add(name, loaded);
            return loaded;
        }
    }
}

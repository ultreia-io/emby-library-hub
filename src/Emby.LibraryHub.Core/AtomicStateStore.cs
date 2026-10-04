using System;
using System.IO;
using System.Text;

namespace Emby.LibraryHub.Core;

public sealed class AtomicStateStore : IDigestStore
{
    private readonly string path;
    private readonly Func<string, DigestState> deserialize;
    private readonly Func<DigestState, string> serialize;

    public AtomicStateStore(string path, Func<string, DigestState> deserialize, Func<DigestState, string> serialize)
    {
        this.path = path;
        this.deserialize = deserialize;
        this.serialize = serialize;
    }

    public DigestState Load()
    {
        if (File.Exists(path)) return deserialize(File.ReadAllText(path, Encoding.UTF8))
            ?? throw new InvalidDataException("Digest state is empty; restore it before continuing.");
        if (File.Exists(path + ".bak"))
            throw new InvalidDataException("Digest state is missing but a backup exists; restore it before continuing.");
        return new DigestState();
    }

    public void Save(DigestState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var data = Encoding.UTF8.GetBytes(serialize(state));
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                       4096, FileOptions.WriteThrough))
            {
                stream.Write(data, 0, data.Length);
                stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}

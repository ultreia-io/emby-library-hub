using System;
using System.IO;
using System.Text.Json;
using Emby.LibraryHub.Core;
using Xunit;

namespace Emby.LibraryHub.Tests;

public sealed class StorageTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "emby-digest-tests-" + Guid.NewGuid());
    private string StatePath => Path.Combine(directory, "state.json");
    private AtomicStateStore Store() => new(StatePath,
        text => JsonSerializer.Deserialize<DigestState>(text)!, state => JsonSerializer.Serialize(state));

    [Fact]
    public void AtomicReplacementRetainsPreviousStateAndSurvivesNewInstance()
    {
        var first = new DigestState(); first.Libraries.Add("first");
        Store().Save(first);
        var second = Store().Load(); second.Libraries.Add("second");
        Store().Save(second);
        Assert.Equal(2, Store().Load().Libraries.Count);
        var backup = JsonSerializer.Deserialize<DigestState>(File.ReadAllText(StatePath + ".bak"))!;
        Assert.Equal("first", Assert.Single(backup.Libraries));
        Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
    }

    [Fact]
    public void CorruptStateFailsClosedWithoutSilentlyCreatingNewBaseline()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(StatePath, "{corrupted");
        Assert.Throws<JsonException>(() => Store().Load());
        Assert.Equal("{corrupted", File.ReadAllText(StatePath));
    }

    [Fact]
    public void MissingMainFileWithExistingBackupRequiresRecovery()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(StatePath + ".bak", "{}");
        Assert.Throws<InvalidDataException>(() => Store().Load());
        Assert.False(File.Exists(StatePath));
    }

    [Fact]
    public void SerializationFailurePreservesCommittedState()
    {
        var store = Store(); var state = new DigestState(); state.Libraries.Add("old"); store.Save(state);
        var broken = new AtomicStateStore(StatePath, text => JsonSerializer.Deserialize<DigestState>(text)!,
            _ => throw new IOException("Serialization failed"));
        Assert.Throws<IOException>(() => broken.Save(new DigestState()));
        Assert.Equal("old", Assert.Single(store.Load().Libraries));
        Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
    }

    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}

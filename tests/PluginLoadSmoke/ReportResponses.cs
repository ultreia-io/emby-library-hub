using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Checks the report HTTP response bodies after real asynchronous waits, using synthetic archive data.</summary>
internal static class ReportResponses
{
    internal static async Task CheckAsync(Assembly plugin, string serverPath)
    {
        Type PluginType(string name) => plugin.GetType("Emby.LibraryHub." + name, true)!;
        var coordinatorType = PluginType("DigestCoordinator");
        var oldCoordinator = coordinatorType.GetProperty("Instance")!.GetValue(null);
        var instance = PluginType("Plugin").GetProperty("Instance")!.GetValue(null)!;
        var statePath = PluginType("Plugin").GetField("<StatePath>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var oldPath = statePath.GetValue(instance);
        var directory = Path.Combine(Path.GetTempPath(), "library-hub-http-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        IDisposable? coordinator = null;
        try
        {
            statePath.SetValue(instance, Path.Combine(directory, "state.json"));
            var day = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
            File.WriteAllText(Path.Combine(directory, "state.json"), JsonSerializer.Serialize(new {
                Version = 1, History = new[] { new { Added = true, ObservedAt = day + "T12:00:00Z",
                    Item = new { LibraryId = "10", LibraryName = "Cinema", ItemId = 1, Path = "/fixture/movie.mkv", Title = "Example", Kind = "Movie" } } }
            }));
            var ready = true;
            var arguments = new List<object>();
            foreach (var parameter in coordinatorType.GetConstructors()[0].GetParameters())
                arguments.Add(HttpProxy.Create(parameter.ParameterType, (method, args) => method.Name switch
                {
                    "get_IsStartupComplete" => ready,
                    "get_IsScanRunning" => false,
                    "get_SystemId" => "fixture-server",
                    "GetVirtualFolders" => Activator.CreateInstance(method.ReturnType),
                    "DeserializeFromString" => JsonSerializer.Deserialize((string)args![0]!, method.GetGenericArguments()[0]),
                    "SerializeToString" => JsonSerializer.Serialize(args![0], args[0]!.GetType()),
                    _ when method.ReturnType == typeof(void) => null,
                    _ => throw new Exception("Unexpected fixture operation: " + method.Name)
                }));
            coordinator = (IDisposable)Activator.CreateInstance(coordinatorType, arguments.ToArray())!;
            var gate = (SemaphoreSlim)coordinatorType.GetField("gate", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(coordinator)!;
            var api = Activator.CreateInstance(PluginType("ReportsAdminApi"))!;
            MethodInfo? unwrap = null;
            var dispatcherPath = Path.Combine(serverPath, "Emby.Server.Implementations.dll");
            if (File.Exists(dispatcherPath))
                unwrap = AssemblyLoadContext.Default.LoadFromAssemblyPath(dispatcherPath)
                    .GetType("Emby.Server.Implementations.Services.ServiceController", true)!
                    .GetMethod("GetTaskResult", BindingFlags.Static | BindingFlags.NonPublic)!;

            async Task<object> Post(string requestType, string from = "", string to = "")
            {
                var type = PluginType(requestType);
                var request = Activator.CreateInstance(type)!;
                type.GetProperty("From")?.SetValue(request, from);
                type.GetProperty("To")?.SetValue(request, to);
                await gate.WaitAsync();
                Task pending;
                try
                {
                    pending = (Task)api.GetType().GetMethod("Post", new[] { type })!.Invoke(api, new[] { request })!;
                    if (pending.IsCompleted) throw new Exception("Report request did not wait for the busy coordinator.");
                }
                finally { gate.Release(); }
                // SDK-only runs enforce Emby's supported async contract. Server-reference runs invoke its real decoder.
                var response = unwrap == null ? await (Task<object>)pending
                    : await (Task<object>)unwrap.Invoke(null, new object[] { pending })!;
                return response ?? throw new Exception("Emby lost the report response after an asynchronous wait.");
            }
            void Require(object result, string property, object expected)
            {
                if (!Equals(result.GetType().GetProperty(property)!.GetValue(result), expected))
                    throw new Exception("Unexpected report response " + property);
            }
            var generated = await Post("GenerateReports", day, day);
            Require(generated, "Count", 1);
            Require(generated, "Path", "emby/LibraryHub/Archive");
            Require(generated, "Error", "");
            if (!File.ReadAllText(Path.Combine(directory, "reports", day + ".html")).Contains("Example"))
                throw new Exception("Generation did not publish the requested report.");
            Require(await Post("GenerateReports", "invalid", day), "Error", "Choose both From and To dates.");
            ready = false;
            Require(await Post("GenerateReports", day, day), "Error", "Emby is starting or scanning. Try generation again when it finishes.");
            Require(await Post("ResetReports"), "Count", 1);
            Console.WriteLine("Report responses: generated HTML, count, invalid dates, busy-server error and reset survive async waits" +
                (unwrap == null ? " (Emby async contract)." : " through the server's actual response decoder."));
        }
        finally
        {
            coordinator?.Dispose();
            coordinatorType.GetProperty("Instance")!.SetValue(null, oldCoordinator);
            statePath.SetValue(instance, oldPath);
            Directory.Delete(directory, true);
        }
    }
}

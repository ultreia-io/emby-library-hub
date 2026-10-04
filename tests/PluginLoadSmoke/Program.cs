using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

// A separate process with no plugin/package references prevents the test runner's
// dependency graph from hiding missing plugin dependency resolution.
var pluginPath = Path.GetFullPath(args[0]);
var serverPath = Path.GetFullPath(args[1]);
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    if (!(name.Name?.StartsWith("MediaBrowser.") == true || name.Name == "Emby.Naming")) return null;
    var path = Path.Combine(serverPath, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
var assembly = Assembly.LoadFile(pluginPath);
_ = assembly.GetTypes();
foreach (var resource in new[] { "config.html", "config.js", "locale.js", "reports.html", "reports.js", "subscriptions.html", "subscriptions.js", "browse.html", "browse.js" })
    if (assembly.GetManifestResourceStream("Emby.LibraryHub.Configuration." + resource) == null)
        throw new Exception("Missing embedded settings resource: " + resource);
var optionsType = assembly.GetType("Emby.LibraryHub.Core.DigestOptions", true)!;
var options = Activator.CreateInstance(optionsType)!;
foreach (var optionEntry in new (string, object)[] {
    ("SmtpHost", "smtp.example.test"), ("SmtpPort", 465), ("UseStartTls", false),
    ("SmtpUsername", "test-user"), ("Sender", "sender@example.test"), ("Recipient", "recipient@example.test") })
    optionsType.GetProperty(optionEntry.Item1)!.SetValue(options, optionEntry.Item2);
var senderType = assembly.GetType("Emby.LibraryHub.Core.SmtpDigestSender", true)!;
_ = Activator.CreateInstance(senderType);
senderType.GetMethod("Validate")!.Invoke(null, new[] { options });
var messageType = assembly.GetType("Emby.LibraryHub.Core.DigestMessage", true)!;
var message = Activator.CreateInstance(messageType)!;
messageType.GetProperty("Id")!.SetValue(message, "load-test");
messageType.GetProperty("Subject")!.SetValue(message, "Médiathèque");
messageType.GetProperty("Body")!.SetValue(message, "Ajouts : Éléphant");
using var mail = (IDisposable)senderType.GetMethod("CreateMessage")!.Invoke(null, new[] { message, options })!;
if (!Equals(mail.GetType().GetProperty("TextBody")!.GetValue(mail), "Ajouts : Éléphant"))
    throw new InvalidDataException("French message did not survive isolated plugin loading.");
Console.WriteLine($"Plugin {assembly.GetName().Version}: isolated loading, SMTP 465 validation and French MIME passed.");

foreach (var name in new[] { "DigestApi", "ReportsAdminApi", "SubscribersAdminApi", "ReportsLinkApi", "CommunityReportsApi", "CommunitySubscriptionsApi", "CommunityUnsubscribeApi", "BrowseApi" })
{
    var attributes = assembly.GetType("Emby.LibraryHub." + name, true)!.GetCustomAttributes(false);
    var auth = attributes.Single(a => a.GetType().Name == "AuthenticatedAttribute");
    if (name is "DigestApi" or "ReportsAdminApi" or "SubscribersAdminApi")
        if (!Equals(auth.GetType().GetProperty("Roles")!.GetValue(auth), "Admin")) throw new Exception(name + " must remain admin-only.");
}
var attributesOnTypes = assembly.GetTypes().SelectMany(t => t.GetCustomAttributes(false)).ToArray();
if (assembly.GetTypes().Any(t => t.Name != "StandalonePagesApi" && t.GetCustomAttributes(false).Any(a => a.GetType().Name == "UnauthenticatedAttribute")))
    throw new Exception("Only the standalone login shell and fixed assets may be anonymous.");
foreach (var resource in new[] { "page.html", "app.js" })
    if (assembly.GetManifestResourceStream("Emby.LibraryHub.Standalone." + resource) == null)
        throw new Exception("Missing standalone resource: " + resource);
var routes = attributesOnTypes.Where(a => a.GetType().Name == "RouteAttribute")
    .Select(a => (string)a.GetType().GetProperty("Path")!.GetValue(a)!).ToArray();
if (routes.Any(path => path.Contains("{ShareId}") || path.StartsWith("/LibraryDigest/History")))
    throw new Exception("Legacy bearer-link or mail endpoints remain reachable.");
foreach (var typeName in new[] { "GetPrivateReport", "GetMySubscription", "UpdateMySubscription", "RemoveMySubscription" })
    if (assembly.GetType("Emby.LibraryHub." + typeName, true)!.GetProperties().Any(p => p.Name.Contains("UserId")))
        throw new Exception("Community routes must not accept a caller-supplied user identity.");
var subscriberInfo = assembly.GetType("Emby.LibraryHub.SubscriberInfo", true)!;
if (subscriberInfo.GetProperties().Any(p => p.Name.Contains("Token") || p.Name.Contains("Hash") || p.Name == "Pending"))
    throw new Exception("Subscriber list must not expose secrets or pending bodies.");
Console.WriteLine("All community routes require authentication; administrator controls, retired links and safe DTOs verified.");

// Use the real target Emby interfaces with synthetic users, folders, and visibility decisions.
var controller = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(serverPath, "MediaBrowser.Controller.dll"));
var model = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(serverPath, "MediaBrowser.Model.dll"));
var userType = controller.GetType("MediaBrowser.Controller.Entities.User", true)!;
var folderType = controller.GetType("MediaBrowser.Controller.Entities.Folder", true)!;
var movieType = controller.GetType("MediaBrowser.Controller.Entities.Movies.Movie", true)!;
var policyType = model.GetType("MediaBrowser.Model.Users.UserPolicy", true)!;
var user = Activator.CreateInstance(userType)!;
var policy = Activator.CreateInstance(policyType)!;
userType.GetProperty("InternalId")!.SetValue(user, 7L);
userType.GetProperty("Policy")!.SetValue(user, policy);
policyType.GetProperty("EnableAllFolders")!.SetValue(policy, false);
policyType.GetProperty("EnabledFolders")!.SetValue(policy, new[] { "10" });
object Item(Type type, long id, string path)
{
    var value = Activator.CreateInstance(type)!;
    type.GetProperty("InternalId")!.SetValue(value, id);
    type.GetProperty("Path")!.SetValue(value, path);
    return value;
}
var folder = Item(folderType, 10, "/media/allowed");
var movie = Item(movieType, 100, "/media/allowed/film.mkv");
var itemVisible = true;
var userExists = true;
var permissionQueries = 0;
var inventoryQueries = 0;
var copies = 1;
var users = HttpProxy.Create(controller.GetType("MediaBrowser.Controller.Library.IUserManager", true)!, (method, values) =>
    method.Name == "GetUserById" ? userExists && (long)values![0]! == 7 ? user : null : throw new Exception(method.Name));
var library = HttpProxy.Create(controller.GetType("MediaBrowser.Controller.Library.ILibraryManager", true)!, (method, values) =>
{
    if (method.Name == "GetInternalId") return long.Parse((string)values![0]!);
    if (method.Name == "GetItemById") return (long)values![0]! == 10 ? folder : (long)values[0]! == 100 ? movie : null;
    if (method.Name == "FilterItemsToIdsForUser")
    {
        permissionQueries++;
        return ((Array)values![0]!).Cast<object>().Select(item => (long)item.GetType().GetProperty("InternalId")!.GetValue(item)!)
            .Where(id => id == 10 || itemVisible).ToArray();
    }
    if (method.Name == "GetItemList")
    {
        inventoryQueries++;
        var query = values![0]!;
        var parentIds = (long[])query.GetType().GetProperty("ParentIds")!.GetValue(query)!;
        if (parentIds.Length != 1 || parentIds[0] != 10) throw new Exception("Unscoped archive inventory query.");
        var array = Array.CreateInstance(method.ReturnType.GetElementType()!, copies);
        for (var n = 0; n < copies; n++)
        {
            var value = n == 0 ? movie : Activator.CreateInstance(movieType)!;
            value.GetType().GetProperty("InternalId")!.SetValue(value, 100L + n);
            value.GetType().GetProperty("Path")!.SetValue(value, n == 0 ? "/media/allowed/film.mkv" : "/media/allowed/" + n + ".mkv");
            array.SetValue(value, n);
        }
        return array;
    }
    if (method.Name == "GetCollectionFolders")
    {
        var array = Array.CreateInstance(method.ReturnType.GetElementType()!, 1); array.SetValue(folder, 0); return array;
    }
    throw new Exception(method.Name);
});
var accessType = assembly.GetType("Emby.LibraryHub.CommunityAccess", true)!;
var access = Activator.CreateInstance(accessType, users, library)!;
bool Account(long id) => (bool)accessType.GetMethod("AccountAllowed")!.Invoke(access, new object[] { id })!;
var entryType = assembly.GetType("Emby.LibraryHub.Core.MediaEntry", true)!;
var entry = Activator.CreateInstance(entryType)!;
entryType.GetProperty("LibraryId")!.SetValue(entry, "10");
entryType.GetProperty("ItemId")!.SetValue(entry, 100L);
entryType.GetProperty("Path")!.SetValue(entry, "/media/allowed/film.mkv");
bool Visible()
{
    var filter = (Delegate)accessType.GetMethod("Filter")!.Invoke(access, new object[] { 7L })!;
    return (bool)filter.DynamicInvoke(entry)!;
}
if (Account(0) || Account(999) || !Account(7) || !Visible()) throw new Exception("Account or allowed-library visibility failed.");
copies = 0; if (Visible()) throw new Exception("Media moved out of the permitted library leaked."); copies = 1;
itemVisible = false; if (Visible()) throw new Exception("Hidden media leaked."); itemVisible = true;
policyType.GetProperty("EnabledFolders")!.SetValue(policy, Array.Empty<string>());
if (Visible()) throw new Exception("Revoked library access leaked.");
policyType.GetProperty("EnabledFolders")!.SetValue(policy, new[] { "10" });
entryType.GetProperty("ItemId")!.SetValue(entry, 999L);
if (!Visible()) throw new Exception("Unrestricted whole-library removal should be visible.");
policyType.GetProperty("MaxParentalRating")!.SetValue(policy, 13);
if (Visible()) throw new Exception("A deleted item with unverifiable parental restrictions leaked.");
var reusedPathFilter = (Delegate)accessType.GetMethod("Filter")!.Invoke(access, new object[] { 7L })!;
entryType.GetProperty("ItemId")!.SetValue(entry, 100L);
if (!(bool)reusedPathFilter.DynamicInvoke(entry)!) throw new Exception("Visible replacement item was blocked.");
entryType.GetProperty("ItemId")!.SetValue(entry, 999L);
if ((bool)reusedPathFilter.DynamicInvoke(entry)!) throw new Exception("A reused media path bypassed deleted-item restrictions.");
policyType.GetProperty("MaxParentalRating")!.SetValue(policy, null);
entryType.GetProperty("ItemId")!.SetValue(entry, 100L);
permissionQueries = 0; inventoryQueries = 0; copies = 20000;
var bulkFilter = (Delegate)accessType.GetMethod("Filter")!.Invoke(access, new object[] { 7L })!;
if (!(bool)bulkFilter.DynamicInvoke(entry)!) throw new Exception("Bulk permission check lost an allowed item.");
for (var n = 1; n < 20000; n++)
{
    entryType.GetProperty("ItemId")!.SetValue(entry, 100L + n);
    entryType.GetProperty("Path")!.SetValue(entry, "/media/allowed/" + n + ".mkv");
    if (!(bool)bulkFilter.DynamicInvoke(entry)!) throw new Exception("Batched visible media was lost.");
}
if (inventoryQueries != 1 || permissionQueries != 80)
    throw new Exception($"Archive permissions did not batch: {inventoryQueries} inventory, {permissionQueries} permission queries.");
Console.WriteLine("20,000-item email permission check: one inventory query, 80 bounded permission queries, no per-entry query loop.");
copies = 1;
policyType.GetProperty("IsDisabled")!.SetValue(policy, true);
if (Account(7)) throw new Exception("Disabled account accepted.");
userExists = false; if (Account(7)) throw new Exception("Deleted account accepted.");
Console.WriteLine("Actual Emby API: account eligibility, library revocation, hidden items and restricted removals passed.");
var coordinatorType = assembly.GetType("Emby.LibraryHub.DigestCoordinator", true)!;
var coordinator = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(coordinatorType);
coordinatorType.GetField("<Access>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(coordinator, access);
coordinatorType.GetProperty("Instance")!.SetValue(null, coordinator);
var authInfoType = controller.GetType("MediaBrowser.Controller.Net.AuthorizationInfo", true)!;
var noUser = Activator.CreateInstance(authInfoType)!;
var authContext = HttpProxy.Create(controller.GetType("MediaBrowser.Controller.Net.IAuthorizationContext", true)!, (_, _) => noUser);
foreach (var contract in new[] { ("CommunityReportsApi", "GetPrivateReport", "Get"),
    ("CommunitySubscriptionsApi", "GetMySubscription", "Get"),
    ("CommunitySubscriptionsApi", "UpdateMySubscription", "Post"),
    ("CommunityUnsubscribeApi", "RemoveMySubscription", "Post") })
{
    var apiType = assembly.GetType("Emby.LibraryHub." + contract.Item1, true)!;
    var api = Activator.CreateInstance(apiType, authContext)!;
    var requestProperty = apiType.GetProperty("Request")!;
    var responseType = requestProperty.PropertyType.GetProperty("Response")!.PropertyType;
    var response = HttpProxy.Create(responseType, (_, _) => null);
    var request = HttpProxy.Create(requestProperty.PropertyType, (method, _) => method.Name == "get_Response" ? response : null);
    requestProperty.SetValue(api, request);
    var dto = Activator.CreateInstance(assembly.GetType("Emby.LibraryHub." + contract.Item2, true)!)!;
    try
    {
        await (System.Threading.Tasks.Task)apiType.GetMethod(contract.Item3)!.Invoke(api, new[] { dto })!;
        throw new Exception("Endpoint accepted a token without a user: " + contract.Item1);
    }
    catch (UnauthorizedAccessException) { }
}
Console.WriteLine("Member report, signup, confirmation and unsubscribe endpoints reject requests without an Emby user.");

// Browsing is read-only, paged, scoped to an Emby account, and filters every returned item.
var browsePluginType = assembly.GetType("Emby.LibraryHub.Plugin", true)!;
var browsePlugin = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(browsePluginType);
var browseConfig = Activator.CreateInstance(assembly.GetType("Emby.LibraryHub.PluginConfiguration", true)!)!;
browsePluginType.BaseType!.GetProperty("Configuration")!.SetValue(browsePlugin, browseConfig);
browsePluginType.GetProperty("Instance")!.SetValue(null, browsePlugin);
var browseOptions = browseConfig.GetType().GetProperty("Digest")!.GetValue(browseConfig)!;
var browseRoot = Item(controller.GetType("MediaBrowser.Controller.Entities.CollectionFolder", true)!, 10, "/media/allowed");
var browseBox = Item(controller.GetType("MediaBrowser.Controller.Entities.BoxSet", true)!, 20, "/collections/example");
var browseCollectionLibrary = Item(controller.GetType("MediaBrowser.Controller.Entities.CollectionFolder", true)!, 30, "/collections");
object BrowseVirtualFolders(Type returnType)
{
    var folders = (System.Collections.IList)Activator.CreateInstance(returnType)!;
    var folderType = model.GetType("MediaBrowser.Model.Entities.VirtualFolderInfo", true)!;
    foreach (var pair in new[] { ("10", "movies"), ("30", "boxsets") })
    {
        var folder = Activator.CreateInstance(folderType)!;
        folderType.GetProperty("ItemId")!.SetValue(folder, pair.Item1);
        folderType.GetProperty("CollectionType")!.SetValue(folder, pair.Item2);
        folders.Add(folder);
    }
    return folders;
}
var queryCount = 0;
var denyBrowseLibrary = false;
object? lastBrowseQuery = null;
var browseLibrary = HttpProxy.Create(controller.GetType("MediaBrowser.Controller.Library.ILibraryManager", true)!, (method, values) =>
{
    if (method.Name == "GetVirtualFolders") return BrowseVirtualFolders(method.ReturnType);
    if (method.Name == "GetInternalId") return long.Parse((string)values![0]!);
    if (method.Name == "GetItemById") return (long)values![0]! == 10 ? browseRoot : (long)values[0]! == 20 ? browseBox : (long)values[0]! == 30 ? browseCollectionLibrary : null;
    if (method.Name == "FilterItemsToIdsForUser")
        return ((Array)values![0]!).Cast<object>().Select(i => (long)i.GetType().GetProperty("InternalId")!.GetValue(i)!).Where(id => id != 101 && (!denyBrowseLibrary || id != 10)).ToArray();
    if (method.Name == "GetItemList")
    {
        lastBrowseQuery = values![0]!; queryCount++;
        if (!ReferenceEquals(lastBrowseQuery.GetType().GetProperty("User")!.GetValue(lastBrowseQuery), user) ||
            ((int?)lastBrowseQuery.GetType().GetProperty("Limit")!.GetValue(lastBrowseQuery) is not (51 or 256))) throw new Exception("Unscoped or unbounded browse query.");
        var types = (string[])lastBrowseQuery.GetType().GetProperty("IncludeItemTypes")!.GetValue(lastBrowseQuery)!;
        if (types.SequenceEqual(new[] { "BoxSet" }))
        {
            if (((long[])lastBrowseQuery.GetType().GetProperty("ParentIds")!.GetValue(lastBrowseQuery)!).Length != 0)
                throw new Exception("Virtual Collections view must not use physical ancestors.");
            var boxes = Array.CreateInstance(method.ReturnType.GetElementType()!, 1); boxes.SetValue(browseBox, 0); return boxes;
        }
        var rows = Array.CreateInstance(method.ReturnType.GetElementType()!, 52);
        for (var n = 0; n < rows.Length; n++)
        {
            var item = Item(movieType, 100L + n, "/media/allowed/" + n);
            movieType.GetProperty("Name")!.SetValue(item, "Example movie " + n);
            rows.SetValue(item, n);
        }
        return rows;
    }
    throw new Exception("Unexpected browse operation: " + method.Name);
});
var browseCatalogType = assembly.GetType("Emby.LibraryHub.BrowseCatalog", true)!;
var browseCatalog = Activator.CreateInstance(browseCatalogType, browseLibrary, user)!;
var browseRequestType = assembly.GetType("Emby.LibraryHub.GetBrowseItems", true)!;
var browseRequest = Activator.CreateInstance(browseRequestType)!;
foreach (var value in new (string, object)[] { ("LibraryId", "10"), ("Search", "example"), ("Genre", "Animation"), ("Year", 2024),
    ("Kind", "Movie"), ("Played", "false"), ("Favorites", true), ("Sort", "CommunityRating"), ("Descending", true) })
    browseRequestType.GetProperty(value.Item1)!.SetValue(browseRequest, value.Item2);
object Browse() => browseCatalogType.GetMethod("Items")!.Invoke(browseCatalog, new[] { browseRequest })!;
var browseResult = Browse();
var browseRows = (Array)browseResult.GetType().GetProperty("Items")!.GetValue(browseResult)!;
if (browseRows.Length != 50 || !Equals(browseResult.GetType().GetProperty("Next")!.GetValue(browseResult), 51))
    throw new Exception("Browse page boundaries or hidden-item filtering failed.");
var browseQueryType = lastBrowseQuery!.GetType();
foreach (var value in new (string, object)[] { ("IsPlayed", false), ("IsFavorite", true), ("Recursive", true) })
    if (!Equals(browseQueryType.GetProperty(value.Item1)!.GetValue(lastBrowseQuery), value.Item2)) throw new Exception("Browse filter lost: " + value.Item1);
if (((string[])browseQueryType.GetProperty("Genres")!.GetValue(lastBrowseQuery)!)[0] != "Animation" ||
    ((int[])browseQueryType.GetProperty("Years")!.GetValue(lastBrowseQuery)!)[0] != 2024) throw new Exception("Browse genre/year filtering failed.");
var listedLibraries = (Array)browseCatalogType.GetMethod("Libraries")!.Invoke(browseCatalog, null)!;
if (listedLibraries.Length != 2 || queryCount != 1 || listedLibraries.Cast<object>().Count(r => (bool)r.GetType().GetProperty("IsCollections")!.GetValue(r)!) != 1)
    throw new Exception("Collections must appear once among libraries, without enumerating its box sets.");
browseRequestType.GetProperty("LibraryId")!.SetValue(browseRequest, "30");
browseRequestType.GetProperty("Mode")!.SetValue(browseRequest, "collections");
var collectionsPage = Browse();
if (((Array)collectionsPage.GetType().GetProperty("Items")!.GetValue(collectionsPage)!).Length != 1 ||
    !string.IsNullOrEmpty((string?)browseQueryType.GetProperty("SearchTerm")!.GetValue(lastBrowseQuery))) throw new Exception("Collections headings must load independently of content filters.");
browseRequestType.GetProperty("Mode")!.SetValue(browseRequest, "contents");
browseRequestType.GetProperty("BoxSetId")!.SetValue(browseRequest, "20");
var collectionTitles = Browse();
if (((Array)collectionTitles.GetType().GetProperty("Items")!.GetValue(collectionTitles)!).Length != 50 ||
    ((long[])browseQueryType.GetProperty("CollectionIds")!.GetValue(lastBrowseQuery)!)[0] != 20 ||
    ((long[])browseQueryType.GetProperty("ParentIds")!.GetValue(lastBrowseQuery)!).Length != 0 ||
    ((string[])browseQueryType.GetProperty("Genres")!.GetValue(lastBrowseQuery)!)[0] != "Animation")
    throw new Exception("Nested collection membership, filters or permissions failed.");
browseRequestType.GetProperty("LibraryId")!.SetValue(browseRequest, "10");
try { Browse(); throw new Exception("A box set was expanded outside the Collections library."); }
catch (TargetInvocationException error) when (error.InnerException is ArgumentException) { }
browseRequestType.GetProperty("BoxSetId")!.SetValue(browseRequest, "");
browseRequestType.GetProperty("Mode")!.SetValue(browseRequest, "");
var beforeDenied = queryCount;
foreach (var rejected in new[] { "20" })
{
    browseRequestType.GetProperty("LibraryId")!.SetValue(browseRequest, rejected);
    try { Browse(); throw new Exception("Individual box set accepted as a library."); }
    catch (TargetInvocationException error) when (error.InnerException is UnauthorizedAccessException) { }
}
denyBrowseLibrary = true;
browseRequestType.GetProperty("LibraryId")!.SetValue(browseRequest, "10");
try { Browse(); throw new Exception("Emby-denied library accepted."); }
catch (TargetInvocationException error) when (error.InnerException is UnauthorizedAccessException) { }
if (queryCount != beforeDenied) throw new Exception("Denied library or box set queried media.");
denyBrowseLibrary = false;
var browseApiType = assembly.GetType("Emby.LibraryHub.BrowseApi", true)!;
var browseApi = Activator.CreateInstance(browseApiType, authContext, browseLibrary, users, null)!;
var browseRequestProperty = browseApiType.GetProperty("Request")!;
var browseResponse = HttpProxy.Create(browseRequestProperty.PropertyType.GetProperty("Response")!.PropertyType, (_, _) => null);
browseRequestProperty.SetValue(browseApi, HttpProxy.Create(browseRequestProperty.PropertyType, (method, _) => method.Name == "get_Response" ? browseResponse : null));
try { browseApiType.GetMethod("Get", new[] { browseRequestType })!.Invoke(browseApi, new[] { browseRequest }); throw new Exception("Anonymous browse accepted."); }
catch (TargetInvocationException error) when (error.InnerException is UnauthorizedAccessException) { }
Console.WriteLine("Browse: authenticated access, library roots, nested collection expansion, box-set scoping, filters, bounded pages, hidden items and Emby library permissions passed.");

// Tree grouping uses real Emby entity types, with a small in-memory library and no server writes.
var entityType = controller.GetType("MediaBrowser.Controller.Entities.BaseItem", true)!;
object TreeItem(Type type, long id, string title, string sort, int? year, float? rating)
{
    var item = Item(type, id, "/media/allowed/" + id);
    entityType.GetProperty("Name")!.SetValue(item, title);
    entityType.GetField("_sortName", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(item, sort);
    entityType.GetProperty("ProductionYear")!.SetValue(item, year);
    entityType.GetProperty("CommunityRating")!.SetValue(item, rating);
    return item;
}
var treeSeries = TreeItem(controller.GetType("MediaBrowser.Controller.Entities.TV.Series", true)!, 200, "Baron Noir", "baron noir", 2024, 8.5f);
var treeSeason = TreeItem(controller.GetType("MediaBrowser.Controller.Entities.TV.Season", true)!, 201, "Season 1", "season 1", 2024, null);
entityType.GetProperty("SeriesId")!.SetValue(treeSeason, 200L); entityType.GetProperty("IndexNumber")!.SetValue(treeSeason, 1);
var treeEpisode = TreeItem(controller.GetType("MediaBrowser.Controller.Entities.TV.Episode", true)!, 202, "Episode title", "episode title", 2024, 9f);
entityType.GetProperty("ParentId")!.SetValue(treeEpisode, 201L); entityType.GetProperty("SeriesId")!.SetValue(treeEpisode, 200L);
entityType.GetProperty("IndexNumber")!.SetValue(treeEpisode, 1);
var treeFilm = TreeItem(movieType, 210, "The Arrival", "arrival", 1999, 10f);
entityType.GetProperty("DateCreated")!.SetValue(treeFilm, new DateTimeOffset(2026, 9, 30, 22, 30, 0, TimeSpan.Zero));
var treeUnknown = TreeItem(movieType, 211, "123", "123", null, null);
var treeHidden = TreeItem(movieType, 212, "Hidden film", "hidden", null, null);
var treeAll = new[] { treeSeries, treeFilm, treeUnknown, treeHidden };
var personInfoType = controller.GetType("MediaBrowser.Controller.Entities.PersonInfo", true)!;
var personRoleType = model.GetType("MediaBrowser.Model.Entities.PersonType", true)!;
var peopleCalls = 0;
var treeQueries = 0;
var nestedSeriesMembershipChecked = false;
var treeLibrary = HttpProxy.Create(controller.GetType("MediaBrowser.Controller.Library.ILibraryManager", true)!, (method, values) =>
{
    if (method.Name == "GetVirtualFolders") return BrowseVirtualFolders(method.ReturnType);
    if (method.Name == "GetInternalId") return long.Parse((string)values![0]!);
    if (method.Name == "GetItemById") return (long)values![0]! switch { 10 => browseRoot, 20 => browseBox, 30 => browseCollectionLibrary, 200 => treeSeries, 201 => treeSeason, _ => null };
    if (method.Name == "FilterItemsToIdsForUser") return ((Array)values![0]!).Cast<object>().Select(i => (long)entityType.GetProperty("InternalId")!.GetValue(i)!).Where(id => id != 212).ToArray();
    if (method.Name == "GetItemPeople")
    {
        peopleCalls++;
        var q = values![0]!; var qt = q.GetType();
        var ids = (long[])qt.GetProperty("ItemIds")!.GetValue(q)!;
        if (ids.Length == 0 || ids.Length > 256 || ids.Contains(212L)) throw new Exception("Credits queried outside accessible batches.");
        var roles = ((Array)qt.GetProperty("PersonTypes")!.GetValue(q)!).Cast<object>().Select(r => r.ToString()).ToArray();
        if (!Equals(qt.GetProperty("EnableIds")!.GetValue(q), true)) throw new Exception("People grouping lost person identifiers.");
        var result = (System.Collections.IList)Activator.CreateInstance(method.ReturnType)!;
        foreach (var credit in new[] { (210L, 300L, "Denis Villeneuve", "Director"), (210L, 301L, "Éva Test", "Director"),
            (200L, 302L, "Daniel Test", "Director"), (210L, 303L, "Amy Adams", "Actor"),
            (210L, 303L, "Amy Adams", "Actor"), (200L, 304L, "Élodie Test", "GuestStar"), (212L, 305L, "Hidden Person", "Actor") })
        {
            if (!ids.Contains(credit.Item1) || !roles.Contains(credit.Item4)) continue;
            var person = Activator.CreateInstance(personInfoType)!;
            foreach (var pair in new (string, object)[] { ("ItemId", credit.Item1), ("Id", credit.Item2), ("Name", credit.Item3),
                ("Type", Enum.Parse(personRoleType, credit.Item4)) }) personInfoType.GetProperty(pair.Item1)!.SetValue(person, pair.Item2);
            result.Add(person);
        }
        return result;
    }
    if (method.Name == "GetItemList")
    {
        treeQueries++; var q = values![0]!; var qt = q.GetType();
        if (!ReferenceEquals(qt.GetProperty("User")!.GetValue(q), user)) throw new Exception("Tree lost its authenticated user.");
        var ids = (long[])qt.GetProperty("ItemIds")!.GetValue(q)!;
        var parents = (long[])qt.GetProperty("ParentIds")!.GetValue(q)!;
        if (ids.Contains(200L) && ((long[])qt.GetProperty("CollectionIds")!.GetValue(q)!).SequenceEqual(new[] { 20L })) nestedSeriesMembershipChecked = true;
        var seriesIds = (long[])qt.GetProperty("SeriesIds")!.GetValue(q)!;
        var types = (string[])qt.GetProperty("IncludeItemTypes")!.GetValue(q)!;
        object[] source = ids.Length > 0 ? ids.Contains(200L) ? new[] { treeSeries } : Array.Empty<object>() :
            seriesIds.Contains(200L) && types.Contains("Season") ? new[] { treeSeason } :
            seriesIds.Contains(200L) && types.Contains("Episode") && Equals(qt.GetProperty("ParentIndexNumber")!.GetValue(q), 1) ? new[] { treeEpisode } :
            parents.Contains(200L) ? new[] { treeSeason } : parents.Contains(201L) ? new[] { treeEpisode } : treeAll;
        var start = (int?)qt.GetProperty("StartIndex")!.GetValue(q) ?? 0;
        var limit = (int?)qt.GetProperty("Limit")!.GetValue(q) ?? 100;
        source = source.Skip(start).Take(limit).ToArray();
        var rows = Array.CreateInstance(entityType, source.Length); for (var n = 0; n < source.Length; n++) rows.SetValue(source[n], n); return rows;
    }
    throw new Exception("Unexpected tree operation: " + method.Name);
});
var treeCatalog = Activator.CreateInstance(browseCatalogType, treeLibrary, user)!;
object Tree(string sort, string mode, string group = "", string parent = "", bool descending = false, string libraryId = "10", string boxId = "")
{
    var req = Activator.CreateInstance(browseRequestType)!;
    foreach (var pair in new (string, object)[] { ("LibraryId", libraryId), ("BoxSetId", boxId), ("Sort", sort), ("Mode", mode), ("Group", group), ("ParentId", parent), ("Descending", descending) }) browseRequestType.GetProperty(pair.Item1)!.SetValue(req, pair.Item2);
    return browseCatalogType.GetMethod("Items")!.Invoke(treeCatalog, new[] { req })!;
}
string[] GroupKeys(object result) => ((Array)result.GetType().GetProperty("Groups")!.GetValue(result)!).Cast<object>().Select(g => (string)g.GetType().GetProperty("Key")!.GetValue(g)!).ToArray();
if (!GroupKeys(Tree("SortName", "groups")).SequenceEqual(new[] { "0-9", "A", "B" })) throw new Exception("Alphabetical tree does not use Emby sort titles.");
if (!GroupKeys(Tree("ProductionYear", "groups", descending: true)).SequenceEqual(new[] { "2020", "1990", "unknown" })) throw new Exception("Decade order or missing values failed.");
if (!GroupKeys(Tree("ProductionYear", "groups", "2020")).SequenceEqual(new[] { "2020/2024" })) throw new Exception("Year subgroup failed.");
if (!GroupKeys(Tree("DateCreated", "groups", "2026")).SequenceEqual(new[] { "2026/10" })) throw new Exception("Local-time month grouping failed.");
if (!GroupKeys(Tree("CommunityRating", "groups", descending: true)).SequenceEqual(new[] { "9", "8", "unknown" })) throw new Exception("Rating bands failed.");
var seasonPage = Tree("SortName", "children", parent: "200");
var seasons = (Array)seasonPage.GetType().GetProperty("Items")!.GetValue(seasonPage)!;
if (seasons.Length != 1 || (string)seasons.GetValue(0)!.GetType().GetProperty("Kind")!.GetValue(seasons.GetValue(0))! != "Season") throw new Exception("Season expansion failed.");
var episodesResult = Tree("SortName", "children", parent: "201");
if (((Array)episodesResult.GetType().GetProperty("Items")!.GetValue(episodesResult)!).Length != 1) throw new Exception("Episode expansion failed.");
var nestedSeasons = Tree("SortName", "children", parent: "200", libraryId: "30", boxId: "20");
if (!nestedSeriesMembershipChecked || ((Array)nestedSeasons.GetType().GetProperty("Items")!.GetValue(nestedSeasons)!).Length != 1)
    throw new Exception("Series expansion lost the selected collection's membership check.");
denyBrowseLibrary = false;
try { Tree("Runtime", "groups"); throw new Exception("Retired duration sorting accepted."); }
catch (TargetInvocationException error) when (error.InnerException is ArgumentException) { }
Console.WriteLine("Browse tree: sort titles, decades/years, local-time months, ratings, unknown-last ordering, and series/season expansion passed.");

if (!GroupKeys(Tree("Director", "groups")).SequenceEqual(new[] { "D", "E", "unknown" })) throw new Exception("Director letters, accents or missing credits failed.");
if (!GroupKeys(Tree("Director", "groups", "D")).SequenceEqual(new[] { "D/302", "D/300" }) ||
    !GroupKeys(Tree("Director", "groups", "D", descending: true)).SequenceEqual(new[] { "D/300", "D/302" })) throw new Exception("People must sort by name, not id.");
if (!GroupKeys(Tree("Actor", "groups", descending: true)).SequenceEqual(new[] { "E", "A", "unknown" })) throw new Exception("Actors, guest stars or unknown-last order failed.");
foreach (var personGroup in new[] { ("Director", "D/300", "210"), ("Director", "E/301", "210"), ("Actor", "A/303", "210"), ("Actor", "unknown", "211") })
{
    var result = Tree(personGroup.Item1, "items", personGroup.Item2);
    var rows = ((Array)result.GetType().GetProperty("Items")!.GetValue(result)!).Cast<object>().ToArray();
    if (rows.Length != 1 || (string)rows[0].GetType().GetProperty("Id")!.GetValue(rows[0])! != personGroup.Item3) throw new Exception("Person membership, duplicate credits or unknown group failed.");
}
var wrongRole = Tree("Director", "items", "A/303");
if (((Array)wrongRole.GetType().GetProperty("Items")!.GetValue(wrongRole)!).Length != 0) throw new Exception("Actor credit leaked into directors.");
if (peopleCalls != 9) throw new Exception("People lookups must be batched per browse request.");
Console.WriteLine("Browse people: directors, actors/guest stars, accents, name ordering, duplicate credits, multiple directors and private batched lookups passed.");

// The standalone route exposes only static login markup, never saved report data.
var factoryType = controller.GetType("MediaBrowser.Controller.Net.IHttpResultFactory", true)!;
var factory = HttpProxy.Create(factoryType, (method, args) =>
{
    if (method.Name != "GetResult" || args![1] is not Stream stream) throw new Exception("Unexpected standalone response.");
    var headers = (System.Collections.Generic.IDictionary<string, string>)args[3]!;
    if (headers["Cache-Control"] != "no-store" || !headers["Content-Security-Policy"].Contains("script-src 'self'"))
        throw new Exception("Missing standalone privacy headers.");
    using var reader = new StreamReader(stream); return reader.ReadToEnd();
});
var pagesType = assembly.GetType("Emby.LibraryHub.StandalonePagesApi", true)!;
var pages = Activator.CreateInstance(pagesType, factory)!;
var pagesRequest = pagesType.GetProperty("Request")!;
var pageUrl = "/proxy/emby/LibraryHub/Archive/2026-10-03";
var pageResponse = HttpProxy.Create(pagesRequest.PropertyType.GetProperty("Response")!.PropertyType, (_, _) => null);
pagesRequest.SetValue(pages, HttpProxy.Create(pagesRequest.PropertyType, (method, _) => method.Name switch
{
    "get_RawUrl" => pageUrl,
    "get_PathInfo" => "/LibraryHub/Archive/2026-10-03",
    "get_Response" => pageResponse,
    _ => null
}));
var pageDto = assembly.GetType("Emby.LibraryHub.GetArchivePage", true)!;
var publicPage = (string)pagesType.GetMethod("Get", new[] { pageDto })!.Invoke(pages, new[] { Activator.CreateInstance(pageDto) })!;
if (!publicPage.Contains("/proxy/emby/LibraryHub/Assets/app.js") || !publicPage.Contains("id=\"loginPanel\"") || publicPage.Contains("saved-report-sentinel"))
    throw new Exception("Standalone shell or reverse-proxy asset paths are invalid.");
if (routes.Any(r => r.StartsWith("/LibraryDigest/"))) throw new Exception("Retired Library Digest routes remain registered.");
Console.WriteLine("Library Hub: only current routes are registered; old routes and redirects are removed.");
Console.WriteLine("Standalone login shell: anonymous HTML only, proxy-safe assets, no-store and CSP verified.");

// Exercise the production archive coordinator with no engine or tracking gate.
// Any attempt to rebuild, reconcile or take the old gate would fail this check.
var archiveDirectory = Path.Combine(Path.GetTempPath(), "digest-read-smoke-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(archiveDirectory);
try
{
    File.WriteAllText(Path.Combine(archiveDirectory, "archive.json"), "{\"Version\":1,\"ShareId\":\"11111111111111111111111111111111\",\"Reports\":[]}");
    File.WriteAllText(Path.Combine(archiveDirectory, "index.html"), "<h1>Saved archive sentinel</h1>");
    var manifestType = assembly.GetType("Emby.LibraryHub.Core.ReportManifest", true)!;
    var deserialize = typeof(ArchiveSmoke).GetMethod("Deserializer")!.MakeGenericMethod(manifestType).Invoke(null, null)!;
    var serialize = typeof(ArchiveSmoke).GetMethod("Serializer")!.MakeGenericMethod(manifestType).Invoke(null, null)!;
    var archive = Activator.CreateInstance(assembly.GetType("Emby.LibraryHub.Core.ReportArchive", true)!, archiveDirectory, deserialize, serialize)!;
    coordinatorType.GetField("<Reports>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(coordinator, archive);
    userExists = true; policyType.GetProperty("IsDisabled")!.SetValue(policy, false);
    permissionQueries = 0; inventoryQueries = 0;
    var read = (System.Threading.Tasks.Task<string?>)coordinatorType.GetMethod("ReadPrivateReportAsync")!.Invoke(coordinator, new object[] { 7L, "" })!;
    if (await read != "<h1>Saved archive sentinel</h1>" || inventoryQueries != 0 || permissionQueries != 0)
        throw new Exception("Archive view must read saved HTML without any media or permission queries.");
    Console.WriteLine("Archive viewing: saved HTML returned with zero media queries and no engine or generation lock.");
}
finally { Directory.Delete(archiveDirectory, true); }

public static class ArchiveSmoke
{
    public static Delegate Deserializer<T>() => new Func<string, T>(text => System.Text.Json.JsonSerializer.Deserialize<T>(text)!);
    public static Delegate Serializer<T>() => new Func<T, string>(value => System.Text.Json.JsonSerializer.Serialize(value));
}


public class HttpProxy : DispatchProxy
{
    public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;
    protected override object? Invoke(MethodInfo? method, object?[]? args) => Handler(method!, args);
    public static object Create(Type type, Func<MethodInfo, object?[]?, object?> handler)
    {
        var proxy = (HttpProxy)DispatchProxy.Create(type, typeof(HttpProxy)); proxy.Handler = handler; return proxy;
    }
}

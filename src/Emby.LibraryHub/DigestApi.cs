using System.Threading.Tasks;
using Emby.LibraryHub.Core;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;

namespace Emby.LibraryHub;

[Route("/LibraryHub/Preview", "GET", Summary = "Preview library changes without sending email.")]
public sealed class GetDigestPreview : IReturn<DigestMessage> { }

[Authenticated(Roles = "Admin")]
public sealed class DigestApi : IService
{
    public async Task<object> Get(GetDigestPreview request) =>
        await DigestCoordinator.Instance.PreviewAsync().ConfigureAwait(false);
}

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
    public Task<DigestMessage> Get(GetDigestPreview request) => DigestCoordinator.Instance.PreviewAsync();
}

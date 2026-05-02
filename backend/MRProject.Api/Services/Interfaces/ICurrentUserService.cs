using MRProject.Api.Common;

namespace MRProject.Api.Services.Interfaces;

public interface ICurrentUserService
{
    CurrentUser GetCurrentUser();
}

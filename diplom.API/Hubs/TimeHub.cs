using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace diplom.API.Hubs
{
    [Authorize]
    public sealed class TimeHub : Hub
    {
    }
}


using diplom.API.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace diplom.API.Services
{
    public interface IUserActivityService
    {
        Task<List<UserActivityDto>> GetUsersActivityAsync(CancellationToken cancellationToken);
    }
}


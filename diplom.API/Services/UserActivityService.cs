using diplom.API.DTOs;
using diplom.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace diplom.API.Services
{
    public sealed class UserActivityService : IUserActivityService
    {
        private readonly AppDbContext _context;

        public UserActivityService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserActivityDto>> GetUsersActivityAsync(CancellationToken cancellationToken)
        {
            var users = await _context.Users
                .Select(u => new { u.Id, u.IsActive })
                .ToListAsync(cancellationToken);

            var activeUserIds = await _context.TimeLogs
                .Where(e => e.EndTime == null)
                .Select(e => e.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var activeSet = activeUserIds.ToHashSet();

            return users
                .Select(u => new UserActivityDto
                {
                    UserId = u.Id,
                    State = !u.IsActive
                        ? UserActivityStateDto.Offline
                        : (activeSet.Contains(u.Id) ? UserActivityStateDto.OnlineActive : UserActivityStateDto.OnlineIdle)
                })
                .ToList();
        }
    }
}


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

        private static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(2);

        public UserActivityService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserActivityDto>> GetUsersActivityAsync(CancellationToken cancellationToken)
        {
            var nowUtc = DateTime.UtcNow;

            var users = await _context.Users
                .Select(u => new { u.Id, u.IsActive, u.LastSeenUtc })
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
                    State = ComputeState(u.IsActive, u.LastSeenUtc, nowUtc, activeSet.Contains(u.Id))
                })
                .ToList();
        }

        private static UserActivityStateDto ComputeState(bool isActiveAccount, DateTime? lastSeenUtc, DateTime nowUtc, bool hasOpenTimeEntry)
        {
            if (!isActiveAccount)
                return UserActivityStateDto.Offline;

            if (!lastSeenUtc.HasValue)
                return UserActivityStateDto.Offline;

            if (nowUtc - lastSeenUtc.Value > OnlineThreshold)
                return UserActivityStateDto.Offline;

            return hasOpenTimeEntry ? UserActivityStateDto.OnlineActive : UserActivityStateDto.OnlineIdle;
        }
    }
}

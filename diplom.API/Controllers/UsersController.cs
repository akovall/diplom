using diplom.Data;
using diplom.Models;
using diplom.Models.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diplom.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/users — admin only
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<User>>> GetAll()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.JobTitle,
                    Role = u.Role.ToString(),
                    u.IsActive
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/users/assignable — for Manager/Admin to populate assignee dropdown
        [HttpGet("assignable")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAssignable()
        {
            var users = await _context.Users
                .Where(u => u.IsActive)
                .Select(u => new { u.Id, u.FullName, u.JobTitle, Role = u.Role.ToString() })
                .ToListAsync();

            return Ok(users);
        }

        // PUT: api/users/5/role
        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] UserRole newRole)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.Role = newRole;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // PUT: api/users/5/deactivate
        [HttpPut("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // PUT: api/users/5/activate
        [HttpPut("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Activate(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.IsActive = true;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}

using diplom.Data;
using diplom.Models;
using diplom.Models.enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diplom.API.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/users
        [HttpGet]
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

        // PUT: api/users/5/role
        [HttpPut("{id}/role")]
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

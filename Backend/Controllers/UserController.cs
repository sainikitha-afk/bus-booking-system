using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;
using System.Security.Cryptography;
using System.Text;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        private static string HashPassword(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }

        [HttpGet]
        public IActionResult GetUsers()
        {
            var users = _context.Users.Select(u => new { u.Id, u.Name, u.Email, u.Role }).ToList();
            return Ok(users);
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto)
        {
            if (_context.Users.Any(u => u.Email == dto.Email))
                return BadRequest("Email already registered");

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Password = HashPassword(dto.Password),
                Role = dto.Role
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            if (dto.Role == "Operator")
            {
                var profile = new OperatorProfile
                {
                    UserId = user.Id,
                    BusinessName = user.Name,
                    IsApproved = false,
                    AppliedAt = DateTime.UtcNow
                };
                _context.OperatorProfiles.Add(profile);
                _context.SaveChanges();
            }

            return Ok(new { user.Id, user.Name, user.Email, user.Role });
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDto loginUser)
        {
            var hashed = HashPassword(loginUser.Password);
            var user = _context.Users
                .FirstOrDefault(u => u.Email == loginUser.Email && u.Password == hashed);

            if (user == null)
                return Unauthorized("Invalid credentials");

            return Ok(new { user.Id, user.Name, user.Email, user.Role });
        }
    }
}

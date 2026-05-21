using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;
using Backend.Exceptions;
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
            try
            {
                var users = _context.Users
                    .Select(u => new { u.Id, u.Name, u.Email, u.Role })
                    .ToList();
                return Ok(users);
            }
            catch (Exception ex)
            {
                throw new BusBookingException("Failed to retrieve users.", ex);
            }
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto)
        {
            try
            {
                if (_context.Users.Any(u => u.Email == dto.Email))
                    return BadRequest("Email already registered");

                var user = new User
                {
                    Name     = dto.Name,
                    Email    = dto.Email,
                    Password = HashPassword(dto.Password),
                    Role     = dto.Role
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                switch (user.Role)
                {
                    case "Operator":
                        var profile = new OperatorProfile
                        {
                            UserId       = user.Id,
                            BusinessName = user.Name,
                            IsApproved   = false,
                            AppliedAt    = DateTime.UtcNow
                        };
                        _context.OperatorProfiles.Add(profile);
                        _context.SaveChanges();
                        break;

                    case "Admin":
                    case "Customer":
                        // no extra setup required
                        break;
                }

                return Ok(new { user.Id, user.Name, user.Email, user.Role });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                // Catches unique-constraint violation if two requests slip past the .Any() check
                throw new BusBookingException("Registration failed — email may already be in use.", ex);
            }
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDto loginUser)
        {
            try
            {
                var hashed = HashPassword(loginUser.Password);
                var user   = _context.Users
                    .FirstOrDefault(u => u.Email == loginUser.Email && u.Password == hashed);

                if (user == null)
                    return Unauthorized("Invalid credentials");

                return Ok(new { user.Id, user.Name, user.Email, user.Role });
            }
            catch (Exception ex)
            {
                throw new BusBookingException("Login failed due to an internal error.", ex);
            }
        }
    }
}

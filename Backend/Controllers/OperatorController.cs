using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;
using System.Security.Cryptography;
using System.Text;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OperatorController(AppDbContext context) : ControllerBase
    {
        private static string Hash(string s) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLower();

        // Register as bus operator (creates User + OperatorProfile, pending approval)
        [HttpPost("register")]
        public IActionResult Register([FromBody] OperatorRegisterDto dto)
        {
            if (context.Users.Any(u => u.Email == dto.Email))
                return BadRequest("Email already registered");

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Password = Hash(dto.Password),
                Role = "Operator"
            };
            context.Users.Add(user);
            context.SaveChanges();

            var profile = new OperatorProfile
            {
                UserId = user.Id,
                BusinessName = dto.BusinessName,
                Phone = dto.Phone,
                IsApproved = false,
                AppliedAt = DateTime.UtcNow
            };
            context.OperatorProfiles.Add(profile);
            context.SaveChanges();

            return Ok(new { message = "Registration submitted. Awaiting admin approval.", userId = user.Id });
        }

        // Get all operators with their profiles (for admin)
        [HttpGet]
        public IActionResult GetAll()
        {
            var operators = context.Users
                .Where(u => u.Role == "Operator")
                .Select(u => new
                {
                    u.Id, u.Name, u.Email,
                    Profile = context.OperatorProfiles
                        .Where(p => p.UserId == u.Id)
                        .Select(p => new { p.BusinessName, p.Phone, p.IsApproved, p.RejectionReason, p.AppliedAt })
                        .FirstOrDefault()
                })
                .ToList();
            return Ok(operators);
        }

        // Get single operator profile
        [HttpGet("{userId}")]
        public IActionResult GetProfile(int userId)
        {
            var user = context.Users.FirstOrDefault(u => u.Id == userId && u.Role == "Operator");
            if (user == null) return NotFound();

            var profile = context.OperatorProfiles.FirstOrDefault(p => p.UserId == userId);
            var routes = context.OperatorRoutes
                .Where(r => r.OperatorUserId == userId)
                .Select(r => new
                {
                    r.Id, r.RouteId, r.OfficeAddress,
                    Route = context.Routes.Where(rt => rt.Id == r.RouteId)
                        .Select(rt => new { rt.SourceName, rt.DestinationName })
                        .FirstOrDefault()
                })
                .ToList();

            return Ok(new { user.Id, user.Name, user.Email, Profile = profile, Routes = routes });
        }

        // Admin approves operator
        [HttpPut("approve/{userId}")]
        public IActionResult Approve(int userId)
        {
            var user = context.Users.FirstOrDefault(u => u.Id == userId && u.Role == "Operator");
            if (user == null) return NotFound("User not found or not an operator");

            var profile = context.OperatorProfiles.FirstOrDefault(p => p.UserId == userId);
            if (profile == null)
            {
                profile = new OperatorProfile
                {
                    UserId = userId,
                    BusinessName = user.Name,
                    IsApproved = false,
                    AppliedAt = DateTime.UtcNow
                };
                context.OperatorProfiles.Add(profile);
            }

            profile.IsApproved = true;
            profile.RejectionReason = null;
            context.SaveChanges();
            return Ok(new { message = "Operator approved" });
        }

        // Admin rejects operator
        [HttpPut("reject/{userId}")]
        public IActionResult Reject(int userId, [FromBody] RejectDto dto)
        {
            var user = context.Users.FirstOrDefault(u => u.Id == userId && u.Role == "Operator");
            if (user == null) return NotFound("User not found or not an operator");

            var profile = context.OperatorProfiles.FirstOrDefault(p => p.UserId == userId);
            if (profile == null)
            {
                profile = new OperatorProfile
                {
                    UserId = userId,
                    BusinessName = user.Name,
                    IsApproved = false,
                    AppliedAt = DateTime.UtcNow
                };
                context.OperatorProfiles.Add(profile);
            }

            profile.IsApproved = false;
            profile.RejectionReason = dto.Reason;
            context.SaveChanges();
            return Ok(new { message = "Operator rejected" });
        }

        // Operator selects a route to operate on
        [HttpPost("route")]
        public IActionResult AddRoute([FromBody] OperatorRoute opRoute)
        {
            var profile = context.OperatorProfiles.FirstOrDefault(p => p.UserId == opRoute.OperatorUserId);
            if (profile == null || !profile.IsApproved)
                return BadRequest("Operator not approved");

            var route = context.Routes.FirstOrDefault(r => r.Id == opRoute.RouteId);
            if (route == null) return BadRequest("Route not found");

            if (context.OperatorRoutes.Any(r => r.OperatorUserId == opRoute.OperatorUserId && r.RouteId == opRoute.RouteId))
                return BadRequest("Already operating this route");

            context.OperatorRoutes.Add(opRoute);
            context.SaveChanges();
            return Ok(opRoute);
        }

        // Get operator's buses
        [HttpGet("{userId}/buses")]
        public IActionResult GetBuses(int userId)
        {
            var buses = context.Buses
                .Where(b => b.OperatorUserId == userId)
                .ToList();
            return Ok(buses);
        }

        // Get operator's revenue summary
        [HttpGet("{userId}/revenue")]
        public IActionResult GetRevenue(int userId)
        {
            var fee = context.PlatformFees.FirstOrDefault(f => f.IsActive);
            var bookings = context.Bookings
                .Where(b => b.PaymentStatus == "Paid")
                .Join(context.Buses.Where(bus => bus.OperatorUserId == userId),
                      b => b.BusId, bus => bus.Id,
                      (b, bus) => new { b, bus })
                .Select(x => new
                {
                    x.b.Id, x.b.SeatNumber, x.b.BookingTime,
                    x.bus.Name, x.bus.Price, x.bus.Source, x.bus.Destination
                })
                .ToList();

            decimal totalRevenue = bookings.Sum(b => b.Price);
            decimal platformCut = fee == null ? 0 :
                fee.FeeType == "Percentage" ? totalRevenue * fee.Amount / 100 :
                bookings.Count * fee.Amount;
            decimal netRevenue = totalRevenue - platformCut;

            return Ok(new
            {
                TotalBookings = bookings.Count,
                TotalRevenue = totalRevenue,
                PlatformFee = platformCut,
                NetRevenue = netRevenue,
                Bookings = bookings
            });
        }
    }

    public class OperatorRegisterDto
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string BusinessName { get; set; } = "";
        public string? Phone { get; set; }
    }

    public class RejectDto
    {
        public string Reason { get; set; } = "";
    }
}

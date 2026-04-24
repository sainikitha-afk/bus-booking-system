using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController(AppDbContext context) : ControllerBase
    {
        // Dashboard stats
        [HttpGet("dashboard")]
        public IActionResult Dashboard()
        {
            return Ok(new
            {
                TotalUsers = context.Users.Count(u => u.Role == "Customer"),
                TotalOperators = context.Users.Count(u => u.Role == "Operator"),
                PendingOperators = context.OperatorProfiles.Count(p => !p.IsApproved),
                TotalBuses = context.Buses.Count(b => b.IsActive),
                TotalBookings = context.Bookings.Count(b => b.Status == "Paid"),
                TotalLocations = context.Locations.Count(),
                TotalRoutes = context.Routes.Count(),
                TotalRevenue = context.Buses
                    .Join(context.Bookings.Where(b => b.PaymentStatus == "Paid"),
                          bus => bus.Id, b => b.BusId,
                          (bus, b) => bus.Price)
                    .Sum()
            });
        }

        // Get / update platform fee
        [HttpGet("platform-fee")]
        public IActionResult GetFee()
        {
            var fee = context.PlatformFees.FirstOrDefault(f => f.IsActive);
            return Ok(fee ?? new PlatformFee { FeeType = "Fixed", Amount = 50 });
        }

        [HttpPut("platform-fee")]
        public IActionResult UpdateFee([FromBody] PlatformFee updated)
        {
            var fee = context.PlatformFees.FirstOrDefault(f => f.IsActive);
            if (fee == null)
            {
                updated.IsActive = true;
                context.PlatformFees.Add(updated);
            }
            else
            {
                fee.FeeType = updated.FeeType;
                fee.Amount = updated.Amount;
            }
            context.SaveChanges();
            return Ok(new { message = "Platform fee updated" });
        }

        // Pending operators awaiting approval
        [HttpGet("pending-operators")]
        public IActionResult PendingOperators()
        {
            var pending = context.OperatorProfiles
                .Where(p => !p.IsApproved)
                .Select(p => new
                {
                    p.UserId, p.BusinessName, p.Phone, p.AppliedAt,
                    User = context.Users.Where(u => u.Id == p.UserId)
                        .Select(u => new { u.Name, u.Email })
                        .FirstOrDefault()
                })
                .ToList();
            return Ok(pending);
        }

        // Add bus on behalf of operator (admin can also add buses)
        [HttpPost("bus")]
        public IActionResult AddBus([FromBody] Bus bus)
        {
            bus.IsActive = true;
            context.Buses.Add(bus);
            context.SaveChanges();
            return Ok(bus);
        }
    }
}

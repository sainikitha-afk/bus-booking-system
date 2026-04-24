using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusController(AppDbContext context) : ControllerBase
    {
        // All active buses (customers)
        [HttpGet]
        public IActionResult GetBuses() =>
            Ok(context.Buses.Where(b => b.IsActive).ToList());

        // Search by source / destination / date
        [HttpGet("search")]
        public IActionResult SearchBuses([FromQuery] string? source, [FromQuery] string? destination, [FromQuery] DateTime? date)
        {
            var query = context.Buses.Where(b => b.IsActive).AsQueryable();

            if (!string.IsNullOrWhiteSpace(source))
                query = query.Where(b => b.Source != null && b.Source.ToLower().Contains(source.ToLower()));

            if (!string.IsNullOrWhiteSpace(destination))
                query = query.Where(b => b.Destination != null && b.Destination.ToLower().Contains(destination.ToLower()));

            if (date.HasValue)
            {
                var utcDate = DateTime.SpecifyKind(date.Value, DateTimeKind.Utc);
                query = query.Where(b => b.TravelDate.HasValue && b.TravelDate.Value.Date == utcDate.Date);
            }

            return Ok(query.ToList());
        }

        [HttpGet("{id}")]
        public IActionResult GetBus(int id)
        {
            var bus = context.Buses.FirstOrDefault(b => b.Id == id);
            if (bus == null) return NotFound();
            return Ok(bus);
        }

        // Operator adds a bus (must be approved, must choose a route they operate)
        [HttpPost]
        public IActionResult AddBus([FromBody] Bus bus)
        {
            if (bus.OperatorUserId.HasValue)
            {
                var profile = context.OperatorProfiles
                    .FirstOrDefault(p => p.UserId == bus.OperatorUserId && p.IsApproved);
                if (profile == null)
                    return BadRequest("Operator not approved");

                if (bus.RouteId.HasValue)
                {
                    var operatesRoute = context.OperatorRoutes.Any(
                        r => r.OperatorUserId == bus.OperatorUserId && r.RouteId == bus.RouteId);
                    if (!operatesRoute)
                        return BadRequest("Operator does not operate this route");

                    var opRoute = context.OperatorRoutes
                        .FirstOrDefault(r => r.OperatorUserId == bus.OperatorUserId && r.RouteId == bus.RouteId);
                    var route = context.Routes.FirstOrDefault(r => r.Id == bus.RouteId);

                    if (route != null)
                    {
                        bus.Source = route.SourceName;
                        bus.Destination = route.DestinationName;
                        bus.BoardingPoint = opRoute?.OfficeAddress ?? bus.BoardingPoint;
                        bus.DropPoint = opRoute?.OfficeAddress ?? bus.DropPoint;
                    }
                }

                // Operator name from user
                var user = context.Users.FirstOrDefault(u => u.Id == bus.OperatorUserId);
                bus.OperatorName = user?.Name;
            }

            bus.IsActive = true;
            bus.SeatsAvailable = bus.TotalSeats;
            if (bus.TravelDate.HasValue)
                bus.TravelDate = DateTime.SpecifyKind(bus.TravelDate.Value, DateTimeKind.Utc);

            context.Buses.Add(bus);
            context.SaveChanges();
            return Ok(bus);
        }

        // Operator updates their bus
        [HttpPut("{id}")]
        public IActionResult UpdateBus(int id, [FromBody] Bus updated)
        {
            var bus = context.Buses.FirstOrDefault(b => b.Id == id);
            if (bus == null) return NotFound();

            bus.Name = updated.Name;
            bus.VehicleRegNumber = updated.VehicleRegNumber;
            bus.Price = updated.Price;
            bus.DepartureTime = updated.DepartureTime;
            bus.ArrivalTime = updated.ArrivalTime;
            bus.BusType = updated.BusType;
            bus.TotalSeats = updated.TotalSeats;
            bus.SeatsAvailable = updated.SeatsAvailable;
            bus.LayoutType = updated.LayoutType;
            bus.IsWomenOnly = updated.IsWomenOnly;
            bus.TravelDate = updated.TravelDate.HasValue
                ? DateTime.SpecifyKind(updated.TravelDate.Value, DateTimeKind.Utc)
                : null;

            context.SaveChanges();
            return Ok(bus);
        }

        // Temporarily disable bus (maintenance)
        [HttpPut("{id}/disable")]
        public IActionResult Disable(int id)
        {
            var bus = context.Buses.FirstOrDefault(b => b.Id == id);
            if (bus == null) return NotFound();
            bus.IsActive = false;
            context.SaveChanges();
            return Ok(new { message = "Bus disabled" });
        }

        // Re-enable bus
        [HttpPut("{id}/enable")]
        public IActionResult Enable(int id)
        {
            var bus = context.Buses.FirstOrDefault(b => b.Id == id);
            if (bus == null) return NotFound();
            bus.IsActive = true;
            context.SaveChanges();
            return Ok(new { message = "Bus enabled" });
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteBus(int id)
        {
            var bus = context.Buses.FirstOrDefault(b => b.Id == id);
            if (bus == null) return NotFound();
            context.Buses.Remove(bus);
            context.SaveChanges();
            return Ok(new { message = "Bus permanently removed" });
        }
    }
}

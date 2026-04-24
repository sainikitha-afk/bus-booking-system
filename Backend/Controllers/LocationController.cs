using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController(AppDbContext context) : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll() =>
            Ok(context.Locations.OrderBy(l => l.CityName).ToList());

        [HttpPost]
        public IActionResult Add([FromBody] Location loc)
        {
            if (string.IsNullOrWhiteSpace(loc.CityName))
                return BadRequest("City name required");

            if (context.Locations.Any(l => l.CityName.ToLower() == loc.CityName.ToLower()))
                return BadRequest("City already exists");

            context.Locations.Add(loc);
            context.SaveChanges();
            return Ok(loc);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var loc = context.Locations.FirstOrDefault(l => l.Id == id);
            if (loc == null) return NotFound();

            // Block if used in any route
            if (context.Routes.Any(r => r.SourceId == id || r.DestinationId == id))
                return BadRequest("Cannot delete: city is used in an existing route");

            context.Locations.Remove(loc);
            context.SaveChanges();
            return Ok(new { message = "Deleted" });
        }
    }
}

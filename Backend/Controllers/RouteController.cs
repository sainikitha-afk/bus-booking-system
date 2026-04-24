using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RouteController(AppDbContext context) : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(context.Routes
                .OrderBy(r => r.SourceName).ThenBy(r => r.DestinationName)
                .Select(r => new { r.Id, r.SourceId, r.DestinationId, r.SourceName, r.DestinationName })
                .ToList());
        }

        // Adding one route automatically creates the reverse route too
        [HttpPost]
        public IActionResult Add([FromBody] BusRoute route)
        {
            var source = context.Locations.FirstOrDefault(l => l.Id == route.SourceId);
            var dest   = context.Locations.FirstOrDefault(l => l.Id == route.DestinationId);

            if (source == null || dest == null)
                return BadRequest("Invalid source or destination");

            if (route.SourceId == route.DestinationId)
                return BadRequest("Source and destination must be different");

            var created = new List<object>();

            if (!context.Routes.Any(r => r.SourceId == route.SourceId && r.DestinationId == route.DestinationId))
            {
                var forward = new BusRoute
                {
                    SourceId = route.SourceId, DestinationId = route.DestinationId,
                    SourceName = source.CityName, DestinationName = dest.CityName
                };
                context.Routes.Add(forward);
                created.Add(new { forward.SourceName, forward.DestinationName });
            }

            // Auto-create reverse route
            if (!context.Routes.Any(r => r.SourceId == route.DestinationId && r.DestinationId == route.SourceId))
            {
                var reverse = new BusRoute
                {
                    SourceId = route.DestinationId, DestinationId = route.SourceId,
                    SourceName = dest.CityName, DestinationName = source.CityName
                };
                context.Routes.Add(reverse);
                created.Add(new { reverse.SourceName, reverse.DestinationName });
            }

            if (created.Count == 0)
                return BadRequest("Route(s) already exist");

            context.SaveChanges();
            return Ok(new { message = $"{created.Count} route(s) created", routes = created });
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var route = context.Routes.FirstOrDefault(r => r.Id == id);
            if (route == null) return NotFound();
            context.Routes.Remove(route);
            context.SaveChanges();
            return Ok(new { message = "Route deleted" });
        }
    }
}

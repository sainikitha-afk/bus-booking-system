using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;
using Backend.Interfaces;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController(AppDbContext context, IEmailService emailService) : ControllerBase
    {
        // ── Submit feedback (one per booking group) ───────────────────────────
        [HttpPost]
        public IActionResult Submit([FromBody] Feedback feedback)
        {
            if (feedback.Rating < 1 || feedback.Rating > 5)
                return BadRequest("Rating must be between 1 and 5");

            // Prevent duplicate feedback for the same booking group
            if (feedback.BookingGroupId.HasValue &&
                context.Feedbacks.Any(f => f.BookingGroupId == feedback.BookingGroupId))
                return Conflict("Feedback already submitted for this booking");

            feedback.SubmittedAt = DateTime.UtcNow;
            context.Feedbacks.Add(feedback);
            context.SaveChanges();

            // Send thank-you email to the user who booked
            var user = context.Users.FirstOrDefault(u => u.Id == feedback.UserId);
            var bus  = context.Buses.FirstOrDefault(b => b.Id == feedback.BusId);

            if (user != null && bus != null)
            {
                var stars    = new string('★', feedback.Rating) + new string('☆', 5 - feedback.Rating);
                var msgHtml  = string.IsNullOrWhiteSpace(feedback.Message)
                    ? "<p><i>No message added.</i></p>"
                    : $"<p><b>Your message:</b> {feedback.Message}</p>";

                var emailBody = $@"
<div style='font-family:sans-serif;max-width:560px;margin:auto;padding:20px'>
  <h2 style='color:#e53935'>Thank you for your feedback! 🙏</h2>
  <p>Hi <b>{user.Name}</b>, we appreciate you taking the time to rate your journey.</p>
  <p><b>Bus:</b> {bus.Name} — {bus.Source} → {bus.Destination}</p>
  <p><b>Your Rating:</b> <span style='font-size:20px;color:#f59e0b'>{stars}</span> ({feedback.Rating}/5)</p>
  {msgHtml}
  <p style='color:#888;font-size:12px;margin-top:20px'>
    Your feedback helps us improve our services. See you on your next journey! 🚌
  </p>
</div>";

                try { emailService.SendBookingEmail(user.Email, "Thanks for your feedback! 🙏", emailBody); }
                catch (Exception ex) { Console.WriteLine("[Email] Feedback email failed: " + ex.Message); }
            }

            return Ok(new { message = "Feedback submitted!", feedbackId = feedback.Id });
        }

        // ── Check if feedback already submitted for a group ───────────────────
        [HttpGet("check/{groupId}")]
        public IActionResult CheckFeedback(int groupId)
        {
            var submitted = context.Feedbacks.Any(f => f.BookingGroupId == groupId);
            return Ok(new { submitted });
        }

        // ── All reviews for a specific bus ────────────────────────────────────
        [HttpGet("bus/{busId}")]
        public IActionResult GetBusReviews(int busId)
        {
            var reviews = context.Feedbacks
                .Where(f => f.BusId == busId)
                .Select(f => new
                {
                    f.Id, f.Rating, f.Message, f.SubmittedAt,
                    UserName = context.Users
                        .Where(u => u.Id == f.UserId)
                        .Select(u => u.Name)
                        .FirstOrDefault()
                })
                .OrderByDescending(f => f.SubmittedAt)
                .ToList();

            return Ok(reviews);
        }

        // ── GROUP BY: average rating + review count for every bus ─────────────
        // SQL: SELECT bus_id, AVG(rating), COUNT(*)
        //      FROM feedbacks
        //      GROUP BY bus_id
        //      ORDER BY AVG(rating) DESC
        [HttpGet("ratings")]
        public IActionResult GetAllRatings()
        {
            var ratings = context.Feedbacks
                .GroupBy(f => f.BusId)
                .Select(g => new
                {
                    BusId        = g.Key,
                    AvgRating    = Math.Round(g.Average(f => (double)f.Rating), 1),
                    TotalReviews = g.Count()
                })
                .OrderByDescending(g => g.AvgRating)
                .ToList();

            return Ok(ratings);
        }

        // ── GROUP BY + HAVING: buses above a minimum rating threshold ─────────
        // SQL: SELECT bus_id, AVG(rating) AS avg_rating, COUNT(*) AS total_reviews
        //      FROM feedbacks
        //      GROUP BY bus_id
        //      HAVING AVG(rating) >= @minRating AND COUNT(*) >= @minCount
        //      ORDER BY avg_rating DESC
        [HttpGet("top-rated")]
        public IActionResult GetTopRated(
            [FromQuery] double minRating = 4.0,
            [FromQuery] int    minCount  = 1)
        {
            var topRated = context.Feedbacks
                .GroupBy(f => f.BusId)
                // .Where() after .GroupBy() is translated to HAVING in SQL by EF Core
                .Where(g => g.Average(f => (double)f.Rating) >= minRating
                         && g.Count() >= minCount)
                .Select(g => new
                {
                    BusId        = g.Key,
                    AvgRating    = Math.Round(g.Average(f => (double)f.Rating), 1),
                    TotalReviews = g.Count(),
                    BusName = context.Buses
                        .Where(b => b.Id == g.Key)
                        .Select(b => b.Name)
                        .FirstOrDefault(),
                    Source = context.Buses
                        .Where(b => b.Id == g.Key)
                        .Select(b => b.Source)
                        .FirstOrDefault(),
                    Destination = context.Buses
                        .Where(b => b.Id == g.Key)
                        .Select(b => b.Destination)
                        .FirstOrDefault()
                })
                .OrderByDescending(g => g.AvgRating)
                .ToList();

            return Ok(topRated);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.Interfaces;
using Backend.Exceptions;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController(AppDbContext context, IEmailService emailService) : ControllerBase
    {
        [HttpGet]
        public IActionResult GetBookings()
        {
            try
            {
                return Ok(context.Bookings.ToList());
            }
            catch (Exception ex)
            {
                throw new BusBookingException("Failed to retrieve bookings.", ex);
            }
        }

        // ── User booking history ──────────────────────────────────────────────
        [HttpGet("user/{userId}")]
        public IActionResult GetUserBookings(int userId)
        {
            try
            {
                var bookings = context.Bookings
                    .Where(b => b.UserId == userId)
                    .Select(b => new
                    {
                        b.Id, b.UserId, b.BusId, b.SeatNumber,
                        b.Status, b.PaymentStatus, b.BookingTime,
                        b.PassengerName, b.Age, b.Gender,
                        b.BookingGroupId,
                        Bus = context.Buses
                            .Where(bus => bus.Id == b.BusId)
                            .Select(bus => new
                            {
                                bus.Name, bus.Source, bus.Destination,
                                bus.TravelDate, bus.DepartureTime, bus.ArrivalTime
                            })
                            .FirstOrDefault()
                    })
                    .OrderByDescending(b => b.BookingTime)
                    .ToList();

                return Ok(bookings);
            }
            catch (Exception ex)
            {
                throw new BusBookingException($"Failed to retrieve bookings for user {userId}.", ex);
            }
        }

        // ── Lock a single seat ────────────────────────────────────────────────
        [HttpPost("lock")]
        public IActionResult LockSeat([FromBody] Booking booking)
        {
            if (booking == null || booking.BusId == 0 || booking.SeatNumber == 0)
                return BadRequest("Invalid booking data");

            try
            {
                var exists = context.Bookings.Any(b =>
                    b.BusId == booking.BusId &&
                    b.SeatNumber == booking.SeatNumber &&
                    (b.Status == "Booked" || b.Status == "Paid" ||
                    (b.Status == "Locked" && b.LockExpiry > DateTime.UtcNow)));

                if (exists)
                    throw new SeatAlreadyTakenException(booking.BusId, booking.SeatNumber);

                booking.Id             = 0;
                booking.BookingGroupId = null;
                booking.Status         = "Locked";
                booking.BookingTime    = DateTime.UtcNow;
                booking.LockExpiry     = DateTime.UtcNow.AddMinutes(5);
                booking.PaymentStatus  = "Pending";

                context.Bookings.Add(booking);
                context.SaveChanges();
                return Ok(booking);
            }
            catch (BusBookingException)
            {
                throw; // let middleware format it — do not swallow domain exceptions
            }
            catch (DbUpdateException ex)
            {
                throw new BusBookingException("Failed to save seat lock. Please retry.", ex);
            }
        }

        // ── Confirm a single booked seat ──────────────────────────────────────
        [HttpPost]
        public IActionResult CreateBooking(Booking booking)
        {
            try
            {
                var locked = context.Bookings.FirstOrDefault(b =>
                    b.BusId == booking.BusId &&
                    b.SeatNumber == booking.SeatNumber &&
                    b.Status == "Locked");

                if (locked == null)
                    return BadRequest("Seat not locked");

                if (locked.LockExpiry < DateTime.UtcNow)
                    throw new SeatLockExpiredException(booking.SeatNumber);

                if (locked.UserId != booking.UserId)
                    return BadRequest("Seat locked by another user");

                locked.Status        = "Booked";
                locked.LockExpiry    = null;
                locked.PassengerName = booking.PassengerName;
                locked.Age           = booking.Age;
                locked.Gender        = booking.Gender;

                context.SaveChanges();
                return Ok(locked);
            }
            catch (BusBookingException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw new BusBookingException("Failed to confirm booking. Please retry.", ex);
            }
        }

        // ── Link individual bookings into a group ─────────────────────────────
        [HttpPost("finalize-group")]
        public async Task<IActionResult> FinalizeGroup([FromBody] FinalizeGroupRequest request)
        {
            if (request.BookingIds == null || request.BookingIds.Count == 0)
                return BadRequest("No booking IDs provided");

            try
            {
                var bookings = context.Bookings
                    .Where(b => request.BookingIds.Contains(b.Id))
                    .ToList();

                if (bookings.Count != request.BookingIds.Count)
                    return BadRequest("Some bookings were not found");

                var group = new BookingGroup
                {
                    UserId        = request.UserId,
                    BusId         = request.BusId,
                    CreatedAt     = DateTime.UtcNow,
                    PaymentStatus = "Pending"
                };

                context.BookingGroups.Add(group);
                await context.SaveChangesAsync();

                foreach (var b in bookings)
                    b.BookingGroupId = group.Id;

                await context.SaveChangesAsync();
                return Ok(new { groupId = group.Id });
            }
            catch (DbUpdateException ex)
            {
                throw new BusBookingException("Failed to create booking group. Please retry.", ex);
            }
        }

        // ── Pay for an entire group — sends ONE confirmation email ────────────
        [HttpPost("pay-group/{groupId}")]
        public async Task<IActionResult> PayGroup(int groupId)
        {
            try
            {
                var group = await context.BookingGroups
                    .Include(g => g.Bookings)
                    .FirstOrDefaultAsync(g => g.Id == groupId);

                if (group == null)
                    throw new BookingNotFoundException(groupId, "Booking group");

                if (group.Bookings.Any(b => b.Status != "Booked"))
                    return BadRequest("All bookings must be in Booked status before payment.");

                foreach (var b in group.Bookings)
                {
                    b.Status        = "Paid";
                    b.PaymentStatus = "Paid";
                }
                group.PaymentStatus = "Paid";
                await context.SaveChangesAsync();

                // Email is non-critical — payment is already saved.
                // Catch EmailDeliveryException specifically so a SMTP failure
                // does NOT roll back the payment.
                var user = context.Users.FirstOrDefault(u => u.Id == group.UserId);
                var bus  = context.Buses.FirstOrDefault(b => b.Id == group.BusId);

                if (user != null && bus != null)
                {
                    var rows = string.Join("", group.Bookings.OrderBy(b => b.SeatNumber).Select(b =>
                        $"<tr>" +
                        $"<td style='padding:6px 12px;border:1px solid #ddd'>{b.SeatNumber}</td>" +
                        $"<td style='padding:6px 12px;border:1px solid #ddd'>{b.PassengerName}</td>" +
                        $"<td style='padding:6px 12px;border:1px solid #ddd'>{b.Age}</td>" +
                        $"<td style='padding:6px 12px;border:1px solid #ddd'>{b.Gender}</td>" +
                        $"</tr>"));

                    var emailBody = $@"
<div style='font-family:sans-serif;max-width:600px;margin:auto;padding:20px'>
  <h2 style='color:#e53935'>Booking Confirmed!</h2>
  <p><b>Bus:</b> {bus.Name} &nbsp;({bus.BusType})</p>
  <p><b>Route:</b> {bus.Source} → {bus.Destination}</p>
  <p><b>Date:</b> {bus.TravelDate?.ToString("dd MMM yyyy")}</p>
  <p><b>Departure:</b> {bus.DepartureTime} &nbsp;&nbsp; <b>Arrival:</b> {bus.ArrivalTime}</p>
  <br/>
  <table style='border-collapse:collapse;width:100%;font-size:14px'>
    <thead>
      <tr style='background:#f5f5f5'>
        <th style='padding:8px 12px;border:1px solid #ddd;text-align:left'>Seat</th>
        <th style='padding:8px 12px;border:1px solid #ddd;text-align:left'>Passenger</th>
        <th style='padding:8px 12px;border:1px solid #ddd;text-align:left'>Age</th>
        <th style='padding:8px 12px;border:1px solid #ddd;text-align:left'>Gender</th>
      </tr>
    </thead>
    <tbody>{rows}</tbody>
  </table>
  <br/>
  <p><b>Total Paid: Rs.{bus.Price * group.Bookings.Count}</b></p>
  <p style='color:#888;font-size:12px'>Thank you for choosing us! Have a safe journey.</p>
</div>";

                    try
                    {
                        emailService.SendBookingEmail(user.Email, "Booking Confirmed", emailBody);
                    }
                    catch (EmailDeliveryException ex)
                    {
                        // Payment succeeded — only email failed. Log and continue.
                        Console.WriteLine($"[Email] Confirmation email failed: {ex.Message}");
                    }
                }

                return Ok(new { message = "Payment successful", groupId });
            }
            catch (BusBookingException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw new BusBookingException("Payment could not be saved. Please retry.", ex);
            }
        }

        // ── Group ticket ──────────────────────────────────────────────────────
        [HttpGet("ticket-group/{groupId}")]
        public IActionResult GetGroupTicket(int groupId)
        {
            try
            {
                var group = context.BookingGroups
                    .Include(g => g.Bookings)
                    .FirstOrDefault(g => g.Id == groupId);

                if (group == null)
                    throw new BookingNotFoundException(groupId, "Booking group");

                var bus  = context.Buses.FirstOrDefault(b => b.Id == group.BusId);
                var user = context.Users.FirstOrDefault(u => u.Id == group.UserId);

                return Ok(new
                {
                    GroupId       = groupId,
                    BusName       = bus?.Name,
                    BusType       = bus?.BusType,
                    Source        = bus?.Source,
                    Destination   = bus?.Destination,
                    TravelDate    = bus?.TravelDate,
                    DepartureTime = bus?.DepartureTime,
                    ArrivalTime   = bus?.ArrivalTime,
                    BoardingPoint = bus?.BoardingPoint,
                    DropPoint     = bus?.DropPoint,
                    BookedBy      = user?.Name,
                    TotalAmount   = (bus?.Price ?? 0) * group.Bookings.Count,
                    PaymentStatus = group.PaymentStatus,
                    BookingTime   = group.CreatedAt,
                    Passengers    = group.Bookings
                        .OrderBy(b => b.SeatNumber)
                        .Select(b => new { b.SeatNumber, b.PassengerName, b.Age, b.Gender })
                });
            }
            catch (BusBookingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BusBookingException($"Failed to load ticket for group {groupId}.", ex);
            }
        }

        // ── Single-seat payment (legacy) ──────────────────────────────────────
        [HttpPost("pay/{id}")]
        public IActionResult MakePayment(int id)
        {
            try
            {
                var booking = context.Bookings.FirstOrDefault(b => b.Id == id);
                if (booking == null)
                    throw new BookingNotFoundException(id);

                if (booking.Status != "Booked")
                    return BadRequest("Booking must be confirmed before payment");

                booking.PaymentStatus = "Paid";
                booking.Status        = "Paid";
                context.SaveChanges();
                return Ok(new { message = "Payment successful", bookingId = booking.Id, seat = booking.SeatNumber });
            }
            catch (BusBookingException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw new BusBookingException($"Failed to record payment for booking {id}.", ex);
            }
        }

        // ── Cancel booking ────────────────────────────────────────────────────
        [HttpPost("cancel/{id}")]
        public IActionResult CancelBooking(int id)
        {
            try
            {
                var booking = context.Bookings.FirstOrDefault(b => b.Id == id);
                if (booking == null)
                    throw new BookingNotFoundException(id);

                if (booking.Status == "Cancelled")
                    return BadRequest("Already cancelled");

                booking.Status        = "Cancelled";
                booking.PaymentStatus = booking.PaymentStatus == "Paid" ? "Refund Initiated" : "Cancelled";
                context.SaveChanges();
                return Ok(booking);
            }
            catch (BusBookingException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                throw new BusBookingException($"Failed to cancel booking {id}.", ex);
            }
        }

        // ── Booked seats for seat map ─────────────────────────────────────────
        [HttpGet("seats/{busId}")]
        public IActionResult GetBookedSeats(int busId)
        {
            try
            {
                var now   = DateTime.UtcNow;
                var seats = context.Bookings
                    .Where(b => b.BusId == busId
                        && b.Status != "Cancelled"
                        && !(b.Status == "Locked" && b.LockExpiry < now))
                    .Select(b => new { b.SeatNumber, b.Status, b.Gender })
                    .ToList();

                return Ok(seats);
            }
            catch (Exception ex)
            {
                throw new BusBookingException($"Failed to load seat map for bus {busId}.", ex);
            }
        }
    }
}

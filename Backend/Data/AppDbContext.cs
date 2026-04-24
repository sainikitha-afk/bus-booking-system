using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Bus> Buses { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<Location> Locations { get; set; } = null!;
        public DbSet<BusRoute> Routes { get; set; } = null!;
        public DbSet<OperatorProfile> OperatorProfiles { get; set; } = null!;
        public DbSet<OperatorRoute> OperatorRoutes { get; set; } = null!;
        public DbSet<PlatformFee> PlatformFees { get; set; } = null!;
        public DbSet<BookingGroup> BookingGroups { get; set; }
    }
}

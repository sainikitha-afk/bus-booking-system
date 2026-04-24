using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Backend.Services;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<EmailService>();
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAngular", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    SeedData(db);
}
app.UseCors("AllowAngular");
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();
app.Run();

static string Hash(string s) =>
    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLower();

static void AddCityIfMissing(AppDbContext db, string name)
{
    if (!db.Locations.Any(l => l.CityName == name))
        db.Locations.Add(new Location { CityName = name });
}

static void AddRouteIfMissing(AppDbContext db, string from, string to)
{
    if (!db.Routes.Any(r => r.SourceName == from && r.DestinationName == to))
        db.Routes.Add(new BusRoute { SourceName = from, DestinationName = to,
            SourceId = db.Locations.First(l => l.CityName == from).Id,
            DestinationId = db.Locations.First(l => l.CityName == to).Id });
}

static void AddBidirectionalRoute(AppDbContext db, string a, string b)
{
    AddRouteIfMissing(db, a, b);
    AddRouteIfMissing(db, b, a);
}

static void SeedData(AppDbContext db)
{
    // ── Users ─────────────────────────────────────────────
    if (!db.Users.Any())
    {
        db.Users.AddRange(
            new User { Name = "Nikitha",  Email = "niki@test.com",      Password = Hash("1234"),     Role = "Customer" },
            new User { Name = "Admin",    Email = "admin@test.com",     Password = Hash("admin123"), Role = "Admin"    },
            new User { Name = "KPN Ops",  Email = "kpn@operator.com",   Password = Hash("kpn123"),   Role = "Operator" },
            new User { Name = "SRS Ops",  Email = "srs@operator.com",   Password = Hash("srs123"),   Role = "Operator" }
        );
        db.SaveChanges();
    }

    // ── Operator Profiles ─────────────────────────────────
    if (!db.OperatorProfiles.Any())
    {
        var kpn = db.Users.First(u => u.Email == "kpn@operator.com");
        var srs = db.Users.First(u => u.Email == "srs@operator.com");
        db.OperatorProfiles.AddRange(
            new OperatorProfile { UserId = kpn.Id, BusinessName = "KPN Travels Pvt Ltd", Phone = "9000000001", IsApproved = true, AppliedAt = DateTime.UtcNow },
            new OperatorProfile { UserId = srs.Id, BusinessName = "SRS Travels",         Phone = "9000000002", IsApproved = true, AppliedAt = DateTime.UtcNow }
        );
        db.SaveChanges();
    }

    // ── Cities (incremental — safe to run on existing DB) ─
    var allCities = new[]
    {
        "Chennai", "Bangalore", "Hyderabad", "Mumbai", "Delhi", "Coimbatore",
        "Pune", "Kolkata", "Agra", "Jaipur", "Ahmedabad", "Surat", "Nagpur",
        "Vadodara", "Bhopal", "Indore", "Chandigarh", "Lucknow", "Patna",
        "Goa", "Kochi", "Mysore", "Madurai", "Trichy", "Visakhapatnam",
        "Vijayawada", "Amritsar", "Jodhpur", "Vellore", "Salem"
    };
    foreach (var city in allCities) AddCityIfMissing(db, city);
    db.SaveChanges();

    // ── Routes (bidirectional, incremental) ───────────────
    var routePairs = new[]
    {
        ("Chennai","Bangalore"), ("Chennai","Hyderabad"), ("Chennai","Coimbatore"),
        ("Chennai","Madurai"),   ("Chennai","Trichy"),    ("Chennai","Kochi"),
        ("Chennai","Vellore"),   ("Chennai","Salem"),     ("Chennai","Mumbai"),
        ("Bangalore","Mumbai"),  ("Bangalore","Mysore"),  ("Bangalore","Kochi"),
        ("Bangalore","Hyderabad"),
        ("Mumbai","Pune"),       ("Mumbai","Goa"),        ("Mumbai","Ahmedabad"),
        ("Mumbai","Nagpur"),
        ("Delhi","Agra"),        ("Delhi","Jaipur"),      ("Delhi","Chandigarh"),
        ("Delhi","Lucknow"),     ("Delhi","Amritsar"),
        ("Hyderabad","Vijayawada"), ("Hyderabad","Visakhapatnam"),
        ("Kolkata","Patna"),
        ("Jaipur","Jodhpur"),    ("Ahmedabad","Surat"),   ("Ahmedabad","Vadodara"),
        ("Bhopal","Indore"),     ("Coimbatore","Kochi"),  ("Madurai","Trichy")
    };
    foreach (var (a, b) in routePairs) AddBidirectionalRoute(db, a, b);
    db.SaveChanges();

    // ── Platform Fee ──────────────────────────────────────
    if (!db.PlatformFees.Any())
    {
        db.PlatformFees.Add(new PlatformFee { FeeType = "Fixed", Amount = 50, IsActive = true });
        db.SaveChanges();
    }

    // ── Sample Buses ──────────────────────────────────────
    if (!db.Buses.Any())
    {
        var kpn  = db.Users.First(u => u.Email == "kpn@operator.com");
        var srs  = db.Users.First(u => u.Email == "srs@operator.com");
        var r1   = db.Routes.First(r => r.SourceName == "Chennai"   && r.DestinationName == "Bangalore");
        var r2   = db.Routes.First(r => r.SourceName == "Chennai"   && r.DestinationName == "Hyderabad");
        var r3   = db.Routes.First(r => r.SourceName == "Bangalore" && r.DestinationName == "Mumbai");
        var r5   = db.Routes.First(r => r.SourceName == "Chennai"   && r.DestinationName == "Coimbatore");
        var r6   = db.Routes.First(r => r.SourceName == "Mumbai"    && r.DestinationName == "Pune");
        var r7   = db.Routes.First(r => r.SourceName == "Delhi"     && r.DestinationName == "Agra");

        db.Buses.AddRange(
            new Bus { Name = "KPN Express",     VehicleRegNumber = "TN01AB1234",
                Source = "Chennai",   Destination = "Bangalore",  RouteId = r1.Id,
                TravelDate = DateTime.UtcNow.Date.AddDays(1), DepartureTime = "10:00 PM", ArrivalTime = "05:00 AM",
                BusType = "AC Sleeper",     OperatorName = "KPN Travels", OperatorUserId = kpn.Id,
                Price = 650,  TotalSeats = 40, SeatsAvailable = 40,
                BoardingPoint = "Koyambedu Bus Stand", DropPoint = "Majestic Bus Terminal",
                LayoutType = "sleeper", IsActive = true },
            new Bus { Name = "SRS Gold",         VehicleRegNumber = "TN02CD5678",
                Source = "Chennai",   Destination = "Hyderabad",  RouteId = r2.Id,
                TravelDate = DateTime.UtcNow.Date.AddDays(1), DepartureTime = "08:00 PM", ArrivalTime = "06:00 AM",
                BusType = "AC Seater",      OperatorName = "SRS Travels",  OperatorUserId = srs.Id,
                Price = 850,  TotalSeats = 40, SeatsAvailable = 40,
                BoardingPoint = "CMBT", DropPoint = "MGBS",
                LayoutType = "2x2", IsActive = true },
            new Bus { Name = "Orange Comfort",   VehicleRegNumber = "KA05EF9012",
                Source = "Bangalore", Destination = "Mumbai",     RouteId = r3.Id,
                TravelDate = DateTime.UtcNow.Date.AddDays(2), DepartureTime = "07:00 PM", ArrivalTime = "09:00 AM",
                BusType = "AC Sleeper",     OperatorName = "KPN Travels", OperatorUserId = kpn.Id,
                Price = 1200, TotalSeats = 36, SeatsAvailable = 36,
                BoardingPoint = "Majestic", DropPoint = "Dadar",
                LayoutType = "sleeper", IsActive = true },
            new Bus { Name = "Parveen Queen",    VehicleRegNumber = "TN03GH3456",
                Source = "Chennai",   Destination = "Coimbatore", RouteId = r5.Id,
                TravelDate = DateTime.UtcNow.Date.AddDays(1), DepartureTime = "11:00 PM", ArrivalTime = "05:30 AM",
                BusType = "AC Seater",      OperatorName = "SRS Travels",  OperatorUserId = srs.Id,
                Price = 450,  TotalSeats = 40, SeatsAvailable = 40,
                BoardingPoint = "Tambaram", DropPoint = "Gandhipuram",
                LayoutType = "2x2", IsWomenOnly = true, IsActive = true },
            new Bus { Name = "Volvo Mumbai Exp",  VehicleRegNumber = "MH12JK7890",
                Source = "Mumbai",    Destination = "Pune",        RouteId = r6.Id,
                TravelDate = DateTime.UtcNow.Date.AddDays(1), DepartureTime = "06:00 AM", ArrivalTime = "09:30 AM",
                BusType = "AC Seater",      OperatorName = "KPN Travels", OperatorUserId = kpn.Id,
                Price = 350,  TotalSeats = 40, SeatsAvailable = 40,
                BoardingPoint = "Dadar Bus Stop", DropPoint = "Shivajinagar",
                LayoutType = "2x2", IsActive = true },
            new Bus { Name = "Agra Link",         VehicleRegNumber = "DL01LM2345",
                Source = "Delhi",     Destination = "Agra",        RouteId = r7.Id,
                TravelDate = DateTime.UtcNow.Date.AddDays(1), DepartureTime = "07:00 AM", ArrivalTime = "11:00 AM",
                BusType = "Non-AC Seater",  OperatorName = "SRS Travels",  OperatorUserId = srs.Id,
                Price = 300,  TotalSeats = 44, SeatsAvailable = 44,
                BoardingPoint = "Kashmiri Gate ISBT", DropPoint = "Agra Fort",
                LayoutType = "2x3", IsActive = true }
        );
        db.SaveChanges();
    }
}

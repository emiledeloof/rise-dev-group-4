using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Admins;
using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Products;
using Rise.Domain.Rooms;
using Rise.Domain.Projects;

namespace Rise.Persistence;
/// <summary>
/// Seeds the database
/// </summary>
/// <param name="dbContext"></param>
/// <param name="roleManager"></param>
/// <param name="userManager"></param>
public class DbSeeder(ApplicationDbContext dbContext, RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
{
    const string PasswordDefault = "A1b2C3!";
    public async Task SeedAsync()
    {
        await RolesAsync();
        await UsersAsync();
        await ProductsAsync();
        await ProjectsAsync();
        await AdminsAsync();
        await RoomsAsync();
        await GuestsAsync();
        await BookingsAsync();
    }

    private async Task RolesAsync()
    {
        if (dbContext.Roles.Any())
            return;

        await roleManager.CreateAsync(new IdentityRole("Administrator"));
        await roleManager.CreateAsync(new IdentityRole("Secretary"));
        await roleManager.CreateAsync(new IdentityRole("Technician"));
    }
    
    private async Task  UsersAsync()
    {
        if (dbContext.Users.Any())
            return;
        
        await dbContext.Roles.ToListAsync();

        var admin = new IdentityUser
        {
            UserName = "admin@example.com",
            Email = "admin@example.com",
            EmailConfirmed = true,
        };
        await userManager.CreateAsync(admin, PasswordDefault);
        
        var secretary = new IdentityUser
        {
            UserName = "secretary@example.com",
            Email = "secretary@example.com",
            EmailConfirmed = true,
        };
        await userManager.CreateAsync(secretary, PasswordDefault);
        
        var technicianAccount1 = new IdentityUser
        {
            UserName = "technician1@example.com",
            Email = "technician1@example.com",
            EmailConfirmed = true,
        };
        await userManager.CreateAsync(technicianAccount1, PasswordDefault);
        
        var technicianAccount2 = new IdentityUser
        {
            UserName = "technician2@example.com",
            Email = "technician2@example.com",
            EmailConfirmed = true,
        };
        await userManager.CreateAsync(technicianAccount2, PasswordDefault);
                
        var user = new IdentityUser
        {
            UserName = "user@example.com",
            Email = "user@example.com",
            EmailConfirmed = true,
        };
        await userManager.CreateAsync(user, PasswordDefault);
        
        await userManager.AddToRoleAsync(admin, "Administrator");
        await userManager.AddToRoleAsync(secretary, "Secretary");
        await userManager.AddToRoleAsync(technicianAccount1, "Technician");
        await userManager.AddToRoleAsync(technicianAccount2, "Technician");

        dbContext.Technicians.AddRange(
            new Technician("Tech 1", "Awesome", technicianAccount1.Id),
            new Technician("Tech 2", "Less Awesome", technicianAccount2.Id));
        
        await dbContext.SaveChangesAsync();
    }
    

    
    private async Task  ProductsAsync()
    {
        if (dbContext.Products.Any())
            return;
        
        dbContext.Products.AddRange(
            new Product{ Name = "Laptop", Description = "15-inch display, 16GB RAM" },
            new Product{ Name = "Smartphone", Description = "6.5-inch screen, 128GB storage" },
            new Product{ Name = "Headphones", Description = "Wireless noise-cancelling" },
            new Product{ Name = "Keyboard", Description = "Mechanical RGB backlit" },
            new Product{ Name = "Mouse", Description = "Ergonomic wireless mouse" },
            new Product{ Name = "Monitor", Description = "27-inch 4K UHD display" },
            new Product{ Name = "Printer", Description = "All-in-one inkjet printer" },
            new Product{ Name = "Camera", Description = "Mirrorless 24MP with 4K video" },
            new Product{ Name = "Smartwatch", Description = "Heart rate monitor, GPS" },
            new Product{ Name = "Speaker", Description = "Bluetooth portable speaker" }
        );

        await dbContext.SaveChangesAsync();
    }
    
    private async Task  ProjectsAsync()
    {
        if (dbContext.Projects.Any())
            return;
        
        var technicians = await dbContext.Technicians.ToListAsync();
        
        if (!technicians.Any())
            return;
        
        var addresses = new List<Address>
        {
            new Address("Koningstraat 12", "Bus 3A", "Brussel", "1000"),
            new Address("Meir 45", "", "Antwerpen", "2000"),
            new Address("Veldstraat 78", "2e verdieping", "Gent", "9000"),
            new Address("Rue de la Loi 175", "", "Bruxelles", "1040"),
            new Address("Place Saint-Lambert 8", "Bureau 12", "Liège", "4000"),
        };

        var rnd = new Random(123); // Using a seed so the random is always the same.
        
        var projects = new List<Project>
        {
            new("Website Redesign", technicians[rnd.Next(technicians.Count)], addresses[0]),
            new("Mobile App Development", technicians[rnd.Next(technicians.Count)], addresses[1]),
            new("Database Migration", technicians[rnd.Next(technicians.Count)], addresses[2]),
            new("E-commerce Platform", technicians[rnd.Next(technicians.Count)], addresses[3]),
            new("CRM Integration", technicians[rnd.Next(technicians.Count)], addresses[4])
        };

        dbContext.Projects.AddRange(projects);
        await dbContext.SaveChangesAsync();
    }

    private async Task AdminsAsync()
    {
        if (dbContext.Admins.Any())
            return;

        var adminAccount = await userManager.FindByEmailAsync("admin@example.com");

        if (adminAccount is null)
            return;

        dbContext.Admins.Add(new Admin(adminAccount.Id));
        await dbContext.SaveChangesAsync();
    }

    private async Task RoomsAsync()
    {
        if (dbContext.Rooms.Any())
            return;

        var single = new Room("101", "Single Room", 1, "Cosy room with a single bed.");
        single.AddRate("Standard", 79m);
        single.AddPhoto("https://picsum.photos/seed/room101/800/600");

        var doubleRoom = new Room("102", "Double Room", 2, "Spacious room with a double bed.");
        doubleRoom.AddRate("Standard", 109m);
        doubleRoom.AddPhoto("https://picsum.photos/seed/room102a/800/600");
        doubleRoom.AddPhoto("https://picsum.photos/seed/room102b/800/600");

        var twin = new Room("103", "Twin Room", 2, "Room with two single beds.");
        twin.AddRate("Standard", 99m);
        twin.AddPhoto("https://picsum.photos/seed/room103/800/600");

        var family = new Room("201", "Family Room", 4, "Large room with a double bed and a bunk bed.");
        family.AddRate("Standard", 159m);
        family.AddPhoto("https://picsum.photos/seed/room201a/800/600");
        family.AddPhoto("https://picsum.photos/seed/room201b/800/600");

        var suite = new Room("301", "Suite", 2, "Suite with a separate living room and a view over the city.");
        suite.AddRate("Standard", 249m);
        suite.AddPhoto("https://picsum.photos/seed/room301/800/600");

        dbContext.Rooms.AddRange(single, doubleRoom, twin, family, suite);
        await dbContext.SaveChangesAsync();
    }

    private async Task GuestsAsync()
    {
        if (dbContext.Guests.Any())
            return;

        dbContext.Guests.AddRange(
            new Guest("Jan Janssens", "jan.janssens@example.com", "+32 470 12 34 56",
                new GuestAddress("Koningstraat 12", "Bus 3A", "Belgium")),
            new Guest("Marie Dubois", "marie.dubois@example.com", "+33 6 12 34 56 78",
                new GuestAddress("Rue de Rivoli 50", null, "France")),
            new Guest("Lukas Müller", "lukas.mueller@example.com", "+49 151 23456789",
                new GuestAddress("Hauptstraße 5", "2. Stock", "Germany")));

        await dbContext.SaveChangesAsync();
    }

    private async Task BookingsAsync()
    {
        if (dbContext.Bookings.Any())
            return;

        var guests = await dbContext.Guests.OrderBy(x => x.Id).ToListAsync();
        var rooms = await dbContext.Rooms.Include(x => x.Rates).OrderBy(x => x.Id).ToListAsync();

        if (guests.Count < 3 || rooms.Count < 5)
            return;

        var paidBooking = new Booking("20261001-0001", guests[0], new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 12));
        paidBooking.AddRoom(rooms[1], rooms[1].Rates[0]);
        var paidPayment = paidBooking.AddPayment(2 * rooms[1].Rates[0].PricePerNight, "cs_test_seed_0001");
        paidPayment.MarkAsPaid(new DateTime(2026, 10, 1, 14, 30, 0, DateTimeKind.Utc), "in_test_seed_0001", "INV-2026-0001");

        var openBooking = new Booking("20261003-0002", guests[1], new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 8));
        openBooking.AddRoom(rooms[0], rooms[0].Rates[0]);
        openBooking.AddRoom(rooms[2], rooms[2].Rates[0]);
        openBooking.AddPayment(3 * (rooms[0].Rates[0].PricePerNight + rooms[2].Rates[0].PricePerNight), "cs_test_seed_0002");

        var familyBooking = new Booking("20261004-0003", guests[2], new DateOnly(2026, 10, 17), new DateOnly(2026, 10, 20));
        familyBooking.AddRoom(rooms[3], rooms[3].Rates[0]);
        var familyPayment = familyBooking.AddPayment(3 * rooms[3].Rates[0].PricePerNight, "cs_test_seed_0003");
        familyPayment.MarkAsPaid(new DateTime(2026, 10, 4, 9, 15, 0, DateTimeKind.Utc), "in_test_seed_0003", "INV-2026-0002");

        // Deposit is paid, the remaining amount is still open.
        var suiteBooking = new Booking("20261005-0004", guests[0], new DateOnly(2026, 12, 23), new DateOnly(2026, 12, 27));
        suiteBooking.AddRoom(rooms[4], rooms[4].Rates[0]);
        var deposit = suiteBooking.AddPayment(300m, "cs_test_seed_0004");
        deposit.MarkAsPaid(new DateTime(2026, 10, 5, 11, 0, 0, DateTimeKind.Utc), "in_test_seed_0004", "INV-2026-0003");
        suiteBooking.AddPayment(4 * rooms[4].Rates[0].PricePerNight - 300m, "cs_test_seed_0005");

        var shortBooking = new Booking("20261006-0005", guests[1], new DateOnly(2026, 10, 24), new DateOnly(2026, 10, 25));
        shortBooking.AddRoom(rooms[1], rooms[1].Rates[0]);
        var shortPayment = shortBooking.AddPayment(rooms[1].Rates[0].PricePerNight, "cs_test_seed_0006");
        shortPayment.MarkAsPaid(new DateTime(2026, 10, 6, 18, 45, 0, DateTimeKind.Utc), "in_test_seed_0006", "INV-2026-0004");

        dbContext.Bookings.AddRange(paidBooking, openBooking, familyBooking, suiteBooking, shortBooking);
        await dbContext.SaveChangesAsync();
    }
}
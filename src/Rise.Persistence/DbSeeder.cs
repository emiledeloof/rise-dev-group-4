using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Admins;
using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;

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
    }

    private async Task UsersAsync()
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

        await userManager.AddToRoleAsync(admin, "Administrator");
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

        // 3 rates per room, the non-refundable one first because the seeded bookings below use Rates[0]
        var single = new Room("101", "Single Room", 1, "Cosy room with a single bed.");
        single.AddRate("Non-refundable", 79m, "No refund after booking");
        single.AddRate("Partially refundable", 89m, "50% refund until 3 days before");
        single.AddRate("Fully refundable", 99m, "100% refund until 7 days before");
        single.AddPhoto("https://picsum.photos/seed/room101/800/600");

        var doubleRoom = new Room("102", "Double Room", 2, "Spacious room with a double bed.");
        doubleRoom.AddRate("Non-refundable", 109m, "No refund after booking");
        doubleRoom.AddRate("Partially refundable", 125m, "50% refund until 3 days before");
        doubleRoom.AddRate("Fully refundable", 139m, "100% refund until 7 days before");
        doubleRoom.AddPhoto("https://picsum.photos/seed/room102a/800/600");
        doubleRoom.AddPhoto("https://picsum.photos/seed/room102b/800/600");

        var twin = new Room("103", "Twin Room", 2, "Room with two single beds.");
        twin.AddRate("Non-refundable", 99m, "No refund after booking");
        twin.AddRate("Partially refundable", 115m, "50% refund until 3 days before");
        twin.AddRate("Fully refundable", 125m, "100% refund until 7 days before");
        twin.AddPhoto("https://picsum.photos/seed/room103/800/600");

        var family = new Room("201", "Family Room", 4, "Large room with a double bed and a bunk bed.");
        family.AddRate("Non-refundable", 159m, "No refund after booking");
        family.AddRate("Partially refundable", 179m, "50% refund until 3 days before");
        family.AddRate("Fully refundable", 199m, "100% refund until 7 days before");
        family.AddPhoto("https://picsum.photos/seed/room201a/800/600");
        family.AddPhoto("https://picsum.photos/seed/room201b/800/600");

        var suite = new Room("301", "Suite", 2, "Suite with a separate living room and a view over the city.");
        suite.AddRate("Non-refundable", 249m, "No refund after booking");
        suite.AddRate("Partially refundable", 285m, "50% refund until 3 days before");
        suite.AddRate("Fully refundable", 315m, "100% refund until 7 days before");
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
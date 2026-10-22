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

        // the 3 real rooms of the estate, texts and photos from the current booking site
        // photos in Rise.Client/wwwroot/images/rooms, the 1st photo is the one on the room card
        // 2 rates per room, non refundable first, the seeded bookings below use Rates[0]
        // deluxe non refundable price = "from" price of the current booking site
        // ! refundable prices are placeholders, a bit above the non refundable one
        // TODO: real standard room price and real refundable prices from the client, asked to the BAs
        var standard = new Room("101", "Standard Room", 2,
            "Twin room with a private bathroom with shower, hairdryer and bidet. Air conditioning, Wi-Fi, flat-screen TV, fridge, coffee machine and tea-maker. Breakfast included, baby cot available, non-smoking.");
        // non-refundable policy to match the name, the default is refundable
        standard.AddRate("Non-refundable", 133m, "No refund after booking").CancellationPolicy = CancellationPolicy.NonRefundable;
        // refundable by default, 100% up to 30 days, 50% up to 7 days
        standard.AddRate("Refundable", 150m);
        for (var i = 1; i <= 4; i++)
            standard.AddPhoto($"images/rooms/standard-room-{i}.jpg");

        var deluxe = new Room("102", "Deluxe Room", 2,
            "Bright room with a California king bed and a high ceiling. Private bathroom with shower, bathrobes, hairdryer and vanity mirror. Air conditioning, central heating, Wi-Fi, flat-screen TV, fridge, coffee machine and tea-maker. Non-smoking.");
        deluxe.AddRate("Non-refundable", 175m, "No refund after booking").CancellationPolicy = CancellationPolicy.NonRefundable;
        deluxe.AddRate("Refundable", 195m);
        for (var i = 1; i <= 5; i++)
            deluxe.AddPhoto($"images/rooms/deluxe-room-{i}.jpg");

        var deluxeBathtub = new Room("103", "Deluxe Room with bathtub", 2,
            "Room with a California king bed, a high ceiling and sound-proofed windows. Private bathroom with bathtub, bathrobes, hairdryer and vanity mirror. Desk, iron, air conditioning, central heating, Wi-Fi, flat-screen TV, fridge, coffee machine and tea-maker. Non-smoking.");
        deluxeBathtub.AddRate("Non-refundable", 175m, "No refund after booking").CancellationPolicy = CancellationPolicy.NonRefundable;
        deluxeBathtub.AddRate("Refundable", 195m);
        for (var i = 1; i <= 4; i++)
            deluxeBathtub.AddPhoto($"images/rooms/deluxe-room-bathtub-{i}.jpg");

        // characteristics and amenities taken from the texts above
        // TODO: size and view from the client, left empty so the page shows no made up values
        standard.BedType = "2 single beds";
        deluxe.BedType = "California king bed";
        deluxeBathtub.BedType = "California king bed";

        // pet and cot rules, only what the texts above say, the page already shows the max nb of guests
        // TODO: pet policy and cots for the deluxe rooms from the client
        standard.Policies = "A baby cot is available on request.\nNon-smoking room.";
        deluxe.Policies = "Non-smoking room.";
        deluxeBathtub.Policies = "Non-smoking room.";

        // shared amenities, icons from Font Awesome (see css/app.css)
        var wifi = new Amenity("Wi-Fi", "fa-wifi");
        var airConditioning = new Amenity("Air conditioning", "fa-snowflake");
        var heating = new Amenity("Central heating", "fa-temperature-high");
        var tv = new Amenity("Flat-screen TV", "fa-tv");
        var fridge = new Amenity("Fridge", "fa-temperature-low");
        var coffeeMachine = new Amenity("Coffee machine", "fa-mug-hot");
        var teaMaker = new Amenity("Tea maker", "fa-mug-saucer");
        var shower = new Amenity("Shower", "fa-shower");
        var bathtub = new Amenity("Bathtub", "fa-bath");
        var hairdryer = new Amenity("Hairdryer", "fa-wind");
        var bathrobes = new Amenity("Bathrobes", "fa-shirt");
        var iron = new Amenity("Iron", "fa-shirt");
        var nonSmoking = new Amenity("Non-smoking", "fa-ban-smoking");

        foreach (var amenity in new[] { wifi, airConditioning, tv, fridge, coffeeMachine, teaMaker, shower, hairdryer, nonSmoking })
            standard.AddAmenity(amenity);
        foreach (var amenity in new[] { wifi, airConditioning, heating, tv, fridge, coffeeMachine, teaMaker, shower, hairdryer, bathrobes, nonSmoking })
            deluxe.AddAmenity(amenity);
        foreach (var amenity in new[] { wifi, airConditioning, heating, tv, fridge, coffeeMachine, teaMaker, bathtub, hairdryer, bathrobes, iron, nonSmoking })
            deluxeBathtub.AddAmenity(amenity);

        dbContext.Rooms.AddRange(standard, deluxe, deluxeBathtub);
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

        // the estate has 3 rooms, rooms[0] standard, rooms[1] deluxe, rooms[2] deluxe with bathtub
        if (guests.Count < 3 || rooms.Count < 3)
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
        familyBooking.AddRoom(rooms[2], rooms[2].Rates[0]);
        var familyPayment = familyBooking.AddPayment(3 * rooms[2].Rates[0].PricePerNight, "cs_test_seed_0003");
        familyPayment.MarkAsPaid(new DateTime(2026, 10, 4, 9, 15, 0, DateTimeKind.Utc), "in_test_seed_0003", "INV-2026-0002");

        // Deposit is paid, the remaining amount is still open.
        var suiteBooking = new Booking("20261005-0004", guests[0], new DateOnly(2026, 12, 23), new DateOnly(2026, 12, 27));
        suiteBooking.AddRoom(rooms[0], rooms[0].Rates[0]);
        var deposit = suiteBooking.AddPayment(300m, "cs_test_seed_0004");
        deposit.MarkAsPaid(new DateTime(2026, 10, 5, 11, 0, 0, DateTimeKind.Utc), "in_test_seed_0004", "INV-2026-0003");
        suiteBooking.AddPayment(4 * rooms[0].Rates[0].PricePerNight - 300m, "cs_test_seed_0005");

        var shortBooking = new Booking("20261006-0005", guests[1], new DateOnly(2026, 10, 24), new DateOnly(2026, 10, 25));
        shortBooking.AddRoom(rooms[1], rooms[1].Rates[0]);
        var shortPayment = shortBooking.AddPayment(rooms[1].Rates[0].PricePerNight, "cs_test_seed_0006");
        shortPayment.MarkAsPaid(new DateTime(2026, 10, 6, 18, 45, 0, DateTimeKind.Utc), "in_test_seed_0006", "INV-2026-0004");

        dbContext.Bookings.AddRange(paidBooking, openBooking, familyBooking, suiteBooking, shortBooking);
        await dbContext.SaveChangesAsync();
    }
}
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Admins;
using Rise.Domain.Bookings;
using Rise.Domain.Common;
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
        // one type per room for now, the estate has 3 different rooms
        var standardType = new RoomType("Standard Room", "Twin room with shower, breakfast included.");
        var deluxeType = new RoomType("Deluxe Room", "Bright room with a California king bed and a shower.");
        var deluxeBathtubType = new RoomType("Deluxe Room with bathtub", "Room with a California king bed and a bathtub.");

        var standard = new Room("101", "Standard Room", 2, standardType,
            "Twin room with a private bathroom with shower, hairdryer and bidet. Air conditioning, Wi-Fi, flat-screen TV, fridge, coffee machine and tea-maker. Breakfast included, baby cot available, non-smoking.");
        // non-refundable policy to match the name, the default is refundable
        standard.AddRate("Non-refundable", 133m, "No refund after booking").CancellationPolicy = CancellationPolicy.NonRefundable;
        // refundable by default, 100% up to 30 days, 50% up to 7 days
        standard.AddRate("Refundable", 150m);
        for (var i = 1; i <= 4; i++)
            standard.AddPhoto($"images/rooms/standard-room-{i}.jpg");

        var deluxe = new Room("102", "Deluxe Room", 2, deluxeType,
            "Bright room with a California king bed and a high ceiling. Private bathroom with shower, bathrobes, hairdryer and vanity mirror. Air conditioning, central heating, Wi-Fi, flat-screen TV, fridge, coffee machine and tea-maker. Non-smoking.");
        deluxe.AddRate("Non-refundable", 175m, "No refund after booking").CancellationPolicy = CancellationPolicy.NonRefundable;
        deluxe.AddRate("Refundable", 195m);
        for (var i = 1; i <= 5; i++)
            deluxe.AddPhoto($"images/rooms/deluxe-room-{i}.jpg");

        var deluxeBathtub = new Room("103", "Deluxe Room with bathtub", 2, deluxeBathtubType,
            "Room with a California king bed, a high ceiling and sound-proofed windows. Private bathroom with bathtub, bathrobes, hairdryer and vanity mirror. Desk, iron, air conditioning, central heating, Wi-Fi, flat-screen TV, fridge, coffee machine and tea-maker. Non-smoking.");
        deluxeBathtub.AddRate("Non-refundable", 175m, "No refund after booking").CancellationPolicy = CancellationPolicy.NonRefundable;
        deluxeBathtub.AddRate("Refundable", 195m);
        for (var i = 1; i <= 4; i++)
            deluxeBathtub.AddPhoto($"images/rooms/deluxe-room-bathtub-{i}.jpg");

        // characteristics and amenities taken from the texts above, the beds belong to the room type
        // TODO: size and view from the client, left empty so the page shows no made up values
        SetBeds(standardType, "2 single beds", "2 eenpersoonsbedden", "2 lits simples", "2 Einzelbetten", "2 letti singoli", "2 łóżka pojedyncze");
        foreach (var deluxeKind in new[] { deluxeType, deluxeBathtubType })
            SetBeds(deluxeKind, "California king bed", "Californian kingsize bed", "Lit California King", "California-King-Bett", "Letto California King", "Łóżko California King");

        // pet and cot rules, only what the texts above say, the page already shows the max nb of guests
        // TODO: pet policy and cots for the deluxe rooms from the client
        standard.Policies = "A baby cot is available on request.\nNon-smoking room.";
        deluxe.Policies = "Non-smoking room.";
        deluxeBathtub.Policies = "Non-smoking room.";

        // shared amenities, icons from Font Awesome (see css/app.css), names in en, nl, fr, de, it, pl
        var wifi = CreateAmenity("fa-wifi", "Wi-Fi", "Wifi", "Wi-Fi", "WLAN", "Wi-Fi", "Wi-Fi");
        var airConditioning = CreateAmenity("fa-snowflake", "Air conditioning", "Airconditioning", "Climatisation", "Klimaanlage", "Aria condizionata", "Klimatyzacja");
        var heating = CreateAmenity("fa-temperature-high", "Central heating", "Centrale verwarming", "Chauffage central", "Zentralheizung", "Riscaldamento centralizzato", "Centralne ogrzewanie");
        var tv = CreateAmenity("fa-tv", "Flat-screen TV", "Flatscreen-tv", "Télévision à écran plat", "Flachbild-TV", "TV a schermo piatto", "Telewizor z płaskim ekranem");
        var fridge = CreateAmenity("fa-temperature-low", "Fridge", "Koelkast", "Réfrigérateur", "Kühlschrank", "Frigorifero", "Lodówka");
        var coffeeMachine = CreateAmenity("fa-mug-hot", "Coffee machine", "Koffiezetapparaat", "Machine à café", "Kaffeemaschine", "Macchina da caffè", "Ekspres do kawy");
        var teaMaker = CreateAmenity("fa-mug-saucer", "Tea maker", "Waterkoker", "Bouilloire", "Wasserkocher", "Bollitore", "Czajnik");
        var shower = CreateAmenity("fa-shower", "Shower", "Douche", "Douche", "Dusche", "Doccia", "Prysznic");
        var bathtub = CreateAmenity("fa-bath", "Bathtub", "Bad", "Baignoire", "Badewanne", "Vasca da bagno", "Wanna");
        var hairdryer = CreateAmenity("fa-wind", "Hairdryer", "Haardroger", "Sèche-cheveux", "Haartrockner", "Asciugacapelli", "Suszarka do włosów");
        var bathrobes = CreateAmenity("fa-shirt", "Bathrobes", "Badjassen", "Peignoirs", "Bademäntel", "Accappatoi", "Szlafroki");
        var iron = CreateAmenity("fa-shirt", "Iron", "Strijkijzer", "Fer à repasser", "Bügeleisen", "Ferro da stiro", "Żelazko");
        var nonSmoking = CreateAmenity("fa-ban-smoking", "Non-smoking", "Rookvrij", "Non-fumeur", "Nichtraucher", "Non fumatori", "Dla niepalących");

        foreach (var amenity in new[] { wifi, airConditioning, tv, fridge, coffeeMachine, teaMaker, shower, hairdryer, nonSmoking })
            standardType.AddAmenity(amenity);
        foreach (var amenity in new[] { wifi, airConditioning, heating, tv, fridge, coffeeMachine, teaMaker, shower, hairdryer, bathrobes, nonSmoking })
            deluxeType.AddAmenity(amenity);
        foreach (var amenity in new[] { wifi, airConditioning, heating, tv, fridge, coffeeMachine, teaMaker, bathtub, hairdryer, bathrobes, iron, nonSmoking })
            deluxeBathtubType.AddAmenity(amenity);

        dbContext.Rooms.AddRange(standard, deluxe, deluxeBathtub);
        await dbContext.SaveChangesAsync();
    }

    // the texts are given in the order of Language.Supported: en, nl, fr, de, it, pl
    private static Amenity CreateAmenity(string icon, params string[] names)
    {
        var amenity = new Amenity(names[0], icon);
        for (var i = 1; i < names.Length; i++)
            amenity.SetName(Language.Supported[i], names[i]);
        return amenity;
    }

    private static void SetBeds(RoomType roomType, params string[] bedConfigurations)
    {
        for (var i = 0; i < bedConfigurations.Length; i++)
            roomType.SetBedConfiguration(Language.Supported[i], bedConfigurations[i]);
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

        var paidBooking = new Booking("20261001-0001", guests[0], new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 12), 2);
        paidBooking.AddRoom(rooms[1], rooms[1].Rates[0]);
        var paidPayment = paidBooking.AddPayment(2 * rooms[1].Rates[0].PricePerNight, "cs_test_seed_0001");
        paidPayment.MarkAsPaid(new DateTime(2026, 10, 1, 14, 30, 0, DateTimeKind.Utc), "in_test_seed_0001", "INV-2026-0001");

        // 3 guests over 2 rooms, a non-refundable and a refundable rate so both policies show on the detail page
        var openBooking = new Booking("20261003-0002", guests[1], new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 8), 3);
        openBooking.AddRoom(rooms[0], rooms[0].Rates[0]);
        openBooking.AddRoom(rooms[2], rooms[2].Rates[1]);
        openBooking.AddPayment(3 * (rooms[0].Rates[0].PricePerNight + rooms[2].Rates[1].PricePerNight), "cs_test_seed_0002");

        var familyBooking = new Booking("20261004-0003", guests[2], new DateOnly(2026, 10, 17), new DateOnly(2026, 10, 20), 2);
        familyBooking.AddRoom(rooms[2], rooms[2].Rates[0]);
        var familyPayment = familyBooking.AddPayment(3 * rooms[2].Rates[0].PricePerNight, "cs_test_seed_0003");
        familyPayment.MarkAsPaid(new DateTime(2026, 10, 4, 9, 15, 0, DateTimeKind.Utc), "in_test_seed_0003", "INV-2026-0002");
        // used during the stay and still open on the final bill
        // VAT is off in the seeded settings, so the rooms have 0 % VAT and only these lines carry VAT
        familyBooking.AddBillLine("Minibar", 3, 6.50m, 22m);
        familyBooking.AddBillLine("Guided vineyard tour", 2, 45m, 22m);

        // Deposit is paid, the remaining amount is still open.
        var suiteBooking = new Booking("20261005-0004", guests[0], new DateOnly(2026, 12, 23), new DateOnly(2026, 12, 27), 2);
        suiteBooking.AddRoom(rooms[0], rooms[0].Rates[1]);
        var deposit = suiteBooking.AddPayment(300m, "cs_test_seed_0004");
        deposit.MarkAsPaid(new DateTime(2026, 10, 5, 11, 0, 0, DateTimeKind.Utc), "in_test_seed_0004", "INV-2026-0003");
        suiteBooking.AddPayment(4 * rooms[0].Rates[1].PricePerNight - 300m, "cs_test_seed_0005");

        var shortBooking = new Booking("20261006-0005", guests[1], new DateOnly(2026, 10, 24), new DateOnly(2026, 10, 25), 1);
        shortBooking.AddRoom(rooms[1], rooms[1].Rates[0]);
        var shortPayment = shortBooking.AddPayment(rooms[1].Rates[0].PricePerNight, "cs_test_seed_0006");
        shortPayment.MarkAsPaid(new DateTime(2026, 10, 6, 18, 45, 0, DateTimeKind.Utc), "in_test_seed_0006", "INV-2026-0004");

        // Paid on the refundable rate, then cancelled by the guest with a full refund.
        var cancelledBooking = new Booking("20261007-0006", guests[2], new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 23), 2);
        cancelledBooking.AddRoom(rooms[1], rooms[1].Rates[1]);
        var cancelledPayment = cancelledBooking.AddPayment(3 * rooms[1].Rates[1].PricePerNight, "cs_test_seed_0007");
        cancelledPayment.MarkAsPaid(new DateTime(2026, 10, 7, 10, 20, 0, DateTimeKind.Utc), "in_test_seed_0007", "INV-2026-0005");
        cancelledBooking.CancelRooms(
            [new RoomCancellation(cancelledBooking.Rooms[0], cancelledBooking.PaidAmount)],
            new DateTime(2026, 10, 12, 16, 5, 0, DateTimeKind.Utc),
            "Guest asked to cancel by phone, flight was cancelled");

        // Paid for 2 rooms with different policies, the refundable room is cancelled and the other one stays booked.
        var partlyCancelledBooking = new Booking("20261008-0007", guests[0], new DateOnly(2027, 2, 12), new DateOnly(2027, 2, 15), 2);
        partlyCancelledBooking.AddRoom(rooms[0], rooms[0].Rates[0]);
        var cancelledRoom = partlyCancelledBooking.AddRoom(rooms[2], rooms[2].Rates[1]);
        var partlyCancelledPayment = partlyCancelledBooking.AddPayment(3 * (rooms[0].Rates[0].PricePerNight + rooms[2].Rates[1].PricePerNight), "cs_test_seed_0008");
        partlyCancelledPayment.MarkAsPaid(new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc), "in_test_seed_0008", "INV-2026-0006");
        partlyCancelledBooking.CancelRooms(
            [new RoomCancellation(cancelledRoom, cancelledRoom.TotalPrice)],
            new DateTime(2026, 10, 20, 9, 30, 0, DateTimeKind.Utc),
            "One of the two couples can no longer travel");

        dbContext.Bookings.AddRange(paidBooking, openBooking, familyBooking, suiteBooking, shortBooking, cancelledBooking, partlyCancelledBooking);
        await dbContext.SaveChangesAsync();
    }
}
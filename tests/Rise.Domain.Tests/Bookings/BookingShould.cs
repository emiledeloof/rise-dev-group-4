using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;

namespace Rise.Domain.Tests.Bookings;

public class BookingShould
{
    private static readonly DateOnly CheckIn = new(2026, 10, 10);
    private static readonly DateOnly CheckOut = new(2026, 10, 12);

    private static Guest CreateGuest()
        => new("Jan Janssens", "jan@example.com", "+32 470 12 34 56", new GuestAddress("Koningstraat 12", null, "Belgium"));

    [Fact]
    public void BeCreated()
    {
        var guest = CreateGuest();

        var booking = new Booking("20261006-0042", guest, CheckIn, CheckOut);

        booking.BookingNumber.ShouldBe("20261006-0042");
        booking.Guest.ShouldBe(guest);
        booking.CheckIn.ShouldBe(CheckIn);
        booking.CheckOut.ShouldBe(CheckOut);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public void NotBeCreatedWithAnInvalidBookingNumber(string? bookingNumber)
    {
        Action act = () => new Booking(bookingNumber!, CreateGuest(), CheckIn, CheckOut);

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NotBeCreatedWhenCheckOutIsNotAfterCheckIn(int nights)
    {
        Action act = () => new Booking("20261006-0042", CreateGuest(), CheckIn, CheckIn.AddDays(nights));

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void CopyThePriceOfTheRateWhenAddingARoom()
    {
        var room = new Room("101", "Single Room", 1);
        var rate = room.AddRate("Standard", 79m);
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut);

        var bookingRoom = booking.AddRoom(room, rate);

        booking.Rooms.ShouldHaveSingleItem().ShouldBe(bookingRoom);
        bookingRoom.PricePerNight.ShouldBe(79m);

        rate.PricePerNight = 99m;
        bookingRoom.PricePerNight.ShouldBe(79m);
    }

    [Fact]
    public void NotAddARoomWithARateOfAnotherRoom()
    {
        var room = new Room("101", "Single Room", 1);
        var otherRoom = new Room("102", "Double Room", 2);
        var otherRate = otherRoom.AddRate("Standard", 109m);
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut);

        Action act = () => booking.AddRoom(room, otherRate);

        act.ShouldThrow<ArgumentException>();
        booking.Rooms.ShouldBeEmpty();
    }

    [Fact]
    public void NotAddTheSameRoomTwice()
    {
        var room = new Room("101", "Single Room", 1);
        var rate = room.AddRate("Standard", 79m);
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut);
        booking.AddRoom(room, rate);

        Action act = () => booking.AddRoom(room, rate);

        act.ShouldThrow<ArgumentException>();
        booking.Rooms.Count.ShouldBe(1);
    }
}

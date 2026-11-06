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

        var booking = new Booking("20261006-0042", guest, CheckIn, CheckOut, 2);

        booking.BookingNumber.ShouldBe("20261006-0042");
        booking.Guest.ShouldBe(guest);
        booking.CheckIn.ShouldBe(CheckIn);
        booking.CheckOut.ShouldBe(CheckOut);
        booking.Adults.ShouldBe(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NotBeCreatedWithoutGuests(int adults)
    {
        Action act = () => new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut, adults);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void CopyTheRateNameWhenAddingARoom()
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));
        var rate = room.AddRate("Standard", 79m);
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut, 1);

        var bookingRoom = booking.AddRoom(room, rate);

        bookingRoom.RateName.ShouldBe("Standard");

        rate.Name = "Saver";
        bookingRoom.RateName.ShouldBe("Standard");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public void NotBeCreatedWithAnInvalidBookingNumber(string? bookingNumber)
    {
        Action act = () => new Booking(bookingNumber!, CreateGuest(), CheckIn, CheckOut, 2);

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NotBeCreatedWhenCheckOutIsNotAfterCheckIn(int nights)
    {
        Action act = () => new Booking("20261006-0042", CreateGuest(), CheckIn, CheckIn.AddDays(nights), 2);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void CopyThePriceOfTheRateWhenAddingARoom()
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));
        var rate = room.AddRate("Standard", 79m);
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut, 2);

        var bookingRoom = booking.AddRoom(room, rate);

        booking.Rooms.ShouldHaveSingleItem().ShouldBe(bookingRoom);
        bookingRoom.PricePerNight.ShouldBe(79m);

        rate.PricePerNight = 99m;
        bookingRoom.PricePerNight.ShouldBe(79m);
    }

    [Fact]
    public void KeepThePricePassedWhenAddingARoom()
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));
        var rate = room.AddRate("Standard", 79m);
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut, 2);

        var bookingRoom = booking.AddRoom(room, rate, 94.80m);

        bookingRoom.PricePerNight.ShouldBe(94.80m);
        rate.PricePerNight.ShouldBe(79m);
    }

    [Fact]
    public void NotAddARoomWithARateOfAnotherRoom()
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));
        var otherRoom = new Room("102", "Double Room", 2, new RoomType("Standard"));
        var otherRate = otherRoom.AddRate("Standard", 109m);
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut, 2);

        Action act = () => booking.AddRoom(room, otherRate);

        act.ShouldThrow<ArgumentException>();
        booking.Rooms.ShouldBeEmpty();
    }

    [Fact]
    public void NotAddTheSameRoomTwice()
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));
        var rate = room.AddRate("Standard", 79m);
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut, 2);
        booking.AddRoom(room, rate);

        Action act = () => booking.AddRoom(room, rate);

        act.ShouldThrow<ArgumentException>();
        booking.Rooms.Count.ShouldBe(1);
    }

    /// <summary>
    /// ! the booking keeps the cancellation policy valid at booking time, a later change on the rate doesnt touch it
    /// </summary>
    [Fact]
    public void CopyTheCancellationPolicyOfTheRateWhenAddingARoom()
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));
        var rate = room.AddRate("Flexible", 79m);
        rate.FullRefundDays = 45;
        rate.PartialRefundDays = 10;
        rate.PartialRefundPercent = 30;
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut, 2);

        var bookingRoom = booking.AddRoom(room, rate);

        bookingRoom.CancellationPolicy.ShouldBe(CancellationPolicy.Refundable);
        bookingRoom.FullRefundDays.ShouldBe(45);
        bookingRoom.PartialRefundDays.ShouldBe(10);
        bookingRoom.PartialRefundPercent.ShouldBe(30);

        // the admin changes the rate after the booking
        rate.CancellationPolicy = CancellationPolicy.NonRefundable;
        rate.FullRefundDays = 60;
        rate.PartialRefundPercent = 0;

        bookingRoom.CancellationPolicy.ShouldBe(CancellationPolicy.Refundable);
        bookingRoom.FullRefundDays.ShouldBe(45);
        bookingRoom.PartialRefundPercent.ShouldBe(30);
    }

    private static readonly DateTime CancelledAt = new(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// a booking for 2 guests, room A (120 for the stay, fits 2) and room B (80 for the stay, fits 1), 200 paid
    /// </summary>
    private static (Booking Booking, BookingRoom A, BookingRoom B) CreatePaidBookingWithTwoRooms(decimal paid = 200m, int adults = 2)
    {
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut, adults);
        var roomA = new Room("101", "Double Room", 2, new RoomType("Standard"));
        var roomB = new Room("102", "Single Room", 1, new RoomType("Standard"));
        var a = booking.AddRoom(roomA, roomA.AddRate("Flexible", 60m));
        var b = booking.AddRoom(roomB, roomB.AddRate("Flexible", 40m));
        booking.AddPayment(paid).MarkAsPaid(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        return (booking, a, b);
    }

    private static void ShouldBeUntouched(Booking booking, int adults = 2)
    {
        booking.ActiveRooms.Count.ShouldBe(booking.Rooms.Count);
        booking.RefundedAmount.ShouldBe(0m);
        booking.Adults.ShouldBe(adults);
    }

    [Fact]
    public void CancelOneRoomAndKeepTheOthersActive()
    {
        var (booking, a, b) = CreatePaidBookingWithTwoRooms();

        booking.CancelRooms([new RoomCancellation(b, 40m)], CancelledAt, "  Guest is ill  ");

        b.IsCancelled.ShouldBeTrue();
        b.CancelledAt.ShouldBe(CancelledAt);
        b.CancellationReason.ShouldBe("Guest is ill");
        b.RefundAmount.ShouldBe(40m);
        a.IsCancelled.ShouldBeFalse();
        a.RefundAmount.ShouldBeNull();
        booking.ActiveRooms.ShouldBe([a]);
        booking.IsPartiallyCancelled.ShouldBeTrue();
        booking.IsCancelled.ShouldBeFalse();
    }

    [Fact]
    public void BeCancelledWhenAllRoomsAreCancelledAtOnce()
    {
        var (booking, a, b) = CreatePaidBookingWithTwoRooms();

        booking.CancelRooms([new RoomCancellation(a, 120m), new RoomCancellation(b, 0m)], CancelledAt, "Guest is ill");

        booking.IsCancelled.ShouldBeTrue();
        booking.IsPartiallyCancelled.ShouldBeFalse();
        booking.ActiveRooms.ShouldBeEmpty();
        booking.RefundedAmount.ShouldBe(120m);
    }

    [Fact]
    public void BeCancelledWhenTheLastRoomIsCancelledLater()
    {
        var (booking, a, b) = CreatePaidBookingWithTwoRooms();
        var later = CancelledAt.AddDays(3);

        booking.CancelRooms([new RoomCancellation(b, 40m)], CancelledAt, "Guest is ill");
        booking.CancelRooms([new RoomCancellation(a, 60m)], later, "Trip is off");

        booking.IsCancelled.ShouldBeTrue();
        booking.RefundedAmount.ShouldBe(100m);
        // * each room keeps its own cancellation
        b.CancelledAt.ShouldBe(CancelledAt);
        b.CancellationReason.ShouldBe("Guest is ill");
        a.CancelledAt.ShouldBe(later);
        a.CancellationReason.ShouldBe("Trip is off");
    }

    [Fact]
    public void KeepItsTermsWhenCancelled()
    {
        var (booking, a, b) = CreatePaidBookingWithTwoRooms();

        booking.CancelRooms([new RoomCancellation(a, 0m), new RoomCancellation(b, 0m)], CancelledAt, "Guest is ill");

        booking.CheckIn.ShouldBe(CheckIn);
        booking.CheckOut.ShouldBe(CheckOut);
        booking.Rooms.Count.ShouldBe(2);
        a.PricePerNight.ShouldBe(60m);
        booking.Payments.Single().IsPaid.ShouldBeTrue();
    }

    [Fact]
    public void NotCancelWithoutRooms()
    {
        var (booking, _, _) = CreatePaidBookingWithTwoRooms();

        Action act = () => booking.CancelRooms([], CancelledAt, "Guest is ill");

        act.ShouldThrow<ArgumentException>();
        ShouldBeUntouched(booking);
    }

    [Fact]
    public void NotCancelTheSameRoomTwiceInOneCancellation()
    {
        var (booking, a, _) = CreatePaidBookingWithTwoRooms();

        Action act = () => booking.CancelRooms([new RoomCancellation(a, 1m), new RoomCancellation(a, 1m)], CancelledAt, "Guest is ill");

        act.ShouldThrow<ArgumentException>();
        ShouldBeUntouched(booking);
    }

    [Fact]
    public void NotCancelARoomOfAnotherBooking()
    {
        var (booking, a, _) = CreatePaidBookingWithTwoRooms();
        var (_, otherRoom, _) = CreatePaidBookingWithTwoRooms();

        Action act = () => booking.CancelRooms([new RoomCancellation(a, 1m), new RoomCancellation(otherRoom, 0m)], CancelledAt, "Guest is ill");

        act.ShouldThrow<ArgumentException>();
        ShouldBeUntouched(booking);
    }

    [Fact]
    public void NotCancelARoomTwice()
    {
        var (booking, a, b) = CreatePaidBookingWithTwoRooms();
        booking.CancelRooms([new RoomCancellation(b, 10m)], CancelledAt, "Guest is ill");

        Action act = () => booking.CancelRooms([new RoomCancellation(a, 5m), new RoomCancellation(b, 5m)], CancelledAt.AddDays(1), "Again");

        act.ShouldThrow<InvalidOperationException>();
        // * the 1st cancellation is kept and the other room of the refused selection stays active
        b.CancellationReason.ShouldBe("Guest is ill");
        b.RefundAmount.ShouldBe(10m);
        a.IsCancelled.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotCancelWithoutReason(string? reason)
    {
        var (booking, _, b) = CreatePaidBookingWithTwoRooms();

        Action act = () => booking.CancelRooms([new RoomCancellation(b, 0m)], CancelledAt, reason!);

        act.ShouldThrow<ArgumentException>();
        ShouldBeUntouched(booking);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(80.01)]
    public void NotRefundOutsideThePriceOfTheRoom(decimal refund)
    {
        var (booking, a, b) = CreatePaidBookingWithTwoRooms();

        // the valid 1st room proves nothing is changed when a later room is refused
        Action act = () => booking.CancelRooms([new RoomCancellation(a, 10m), new RoomCancellation(b, refund)], CancelledAt, "Guest is ill");

        act.ShouldThrow<ArgumentException>();
        ShouldBeUntouched(booking);
    }

    [Fact]
    public void RefundExactlyThePriceOfTheRoom()
    {
        var (booking, _, b) = CreatePaidBookingWithTwoRooms();

        booking.CancelRooms([new RoomCancellation(b, 80m)], CancelledAt, "Guest is ill");

        b.RefundAmount.ShouldBe(80m);
    }

    [Fact]
    public void NotRefundMoreThanTheDepositPaid()
    {
        var (booking, a, b) = CreatePaidBookingWithTwoRooms(paid: 100m);

        Action act = () => booking.CancelRooms([new RoomCancellation(a, 60m), new RoomCancellation(b, 40.01m)], CancelledAt, "Guest is ill");

        act.ShouldThrow<ArgumentException>();
        ShouldBeUntouched(booking);
    }

    [Fact]
    public void NotRefundMoreThanPaidOverSeveralCancellations()
    {
        var (booking, a, b) = CreatePaidBookingWithTwoRooms(paid: 100m);
        booking.CancelRooms([new RoomCancellation(b, 80m)], CancelledAt, "Guest is ill");

        Action act = () => booking.CancelRooms([new RoomCancellation(a, 20.01m)], CancelledAt, "Trip is off");

        act.ShouldThrow<ArgumentException>();
        a.IsCancelled.ShouldBeFalse();
        booking.RefundedAmount.ShouldBe(80m);
    }

    [Fact]
    public void RefundWhatIsLeftOverSeveralCancellations()
    {
        var (booking, a, b) = CreatePaidBookingWithTwoRooms(paid: 100m);
        booking.CancelRooms([new RoomCancellation(b, 80m)], CancelledAt, "Guest is ill");

        booking.CancelRooms([new RoomCancellation(a, 20m)], CancelledAt, "Trip is off");

        booking.RefundedAmount.ShouldBe(100m);
    }

    [Fact]
    public void NotRefundAnUnpaidPayment()
    {
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut, 2);
        var room = new Room("101", "Double Room", 2, new RoomType("Standard"));
        var bookingRoom = booking.AddRoom(room, room.AddRate("Flexible", 60m));
        booking.AddPayment(120m);

        Action act = () => booking.CancelRooms([new RoomCancellation(bookingRoom, 1m)], CancelledAt, "Guest is ill");

        booking.PaidAmount.ShouldBe(0m);
        act.ShouldThrow<ArgumentException>();
        bookingRoom.IsCancelled.ShouldBeFalse();
    }

    [Fact]
    public void NotKeepMoreGuestsThanTheRemainingRoomsFit()
    {
        // 2 guests, only room B that fits 1 stays
        var (booking, a, _) = CreatePaidBookingWithTwoRooms();

        Action act = () => booking.CancelRooms([new RoomCancellation(a, 0m)], CancelledAt, "Guest is ill");

        act.ShouldThrow<ArgumentException>();
        ShouldBeUntouched(booking);
    }

    [Fact]
    public void NotAcceptANewGuestCountAboveTheRemainingCapacity()
    {
        var (booking, a, _) = CreatePaidBookingWithTwoRooms(adults: 3);

        Action act = () => booking.CancelRooms([new RoomCancellation(a, 0m)], CancelledAt, "Guest is ill", adults: 2);

        act.ShouldThrow<ArgumentException>();
        ShouldBeUntouched(booking, adults: 3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NotAcceptANewGuestCountBelowOne(int adults)
    {
        var (booking, _, b) = CreatePaidBookingWithTwoRooms();

        Action act = () => booking.CancelRooms([new RoomCancellation(b, 0m)], CancelledAt, "Guest is ill", adults);

        act.ShouldThrow<ArgumentException>();
        ShouldBeUntouched(booking);
    }

    [Fact]
    public void SaveTheNewGuestCountOfTheRemainingRooms()
    {
        var (booking, a, _) = CreatePaidBookingWithTwoRooms();

        booking.CancelRooms([new RoomCancellation(a, 0m)], CancelledAt, "Guest is ill", adults: 1);

        booking.Adults.ShouldBe(1);
    }

    [Fact]
    public void KeepTheGuestCountWhenGuestsStillFit()
    {
        var (booking, _, b) = CreatePaidBookingWithTwoRooms();

        booking.CancelRooms([new RoomCancellation(b, 0m)], CancelledAt, "Guest is ill");

        booking.Adults.ShouldBe(2);
    }

    /// <summary>
    /// with no rooms left the nb of guests is only history, even when a new nb is sent
    /// </summary>
    [Fact]
    public void KeepTheGuestCountWhenCancellingAllRooms()
    {
        var (booking, a, b) = CreatePaidBookingWithTwoRooms(adults: 3);

        booking.CancelRooms([new RoomCancellation(a, 0m), new RoomCancellation(b, 0m)], CancelledAt, "Guest is ill", adults: 1);

        booking.Adults.ShouldBe(3);
    }

    [Fact]
    public void NotCountDeletedRoomsAsActive()
    {
        var (booking, a, b) = CreatePaidBookingWithTwoRooms();
        a.IsDeleted = true;

        booking.ActiveRooms.ShouldBe([b]);

        booking.CancelRooms([new RoomCancellation(b, 0m)], CancelledAt, "Guest is ill");

        // the deleted room doesn't keep the booking open
        booking.IsCancelled.ShouldBeTrue();
    }

    [Fact]
    public void NotBeCancelledWithoutRooms()
    {
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut, 2);

        booking.IsCancelled.ShouldBeFalse();
        booking.IsPartiallyCancelled.ShouldBeFalse();
    }

    [Fact]
    public void CalculateNightsAndRoomTotal()
    {
        var (booking, a, _) = CreatePaidBookingWithTwoRooms();

        booking.Nights.ShouldBe(2);
        a.TotalPrice.ShouldBe(120m);
    }

    [Fact]
    public void ChangeTheAmountOfAnUnpaidPayment()
    {
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut, 2);
        var payment = booking.AddPayment(200m);

        payment.ChangeAmount(120m);

        payment.Amount.ShouldBe(120m);
    }

    [Fact]
    public void NotChangeTheAmountOfAPaidPayment()
    {
        var (booking, _, _) = CreatePaidBookingWithTwoRooms();
        var payment = booking.Payments.Single();

        Action act = () => payment.ChangeAmount(120m);

        act.ShouldThrow<InvalidOperationException>();
        payment.Amount.ShouldBe(200m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NotChangeThePaymentToANonPositiveAmount(decimal amount)
    {
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut, 2);
        var payment = booking.AddPayment(200m);

        Action act = () => payment.ChangeAmount(amount);

        act.ShouldThrow<ArgumentException>();
        payment.Amount.ShouldBe(200m);
    }

    [Fact]
    public void KeepTheVatPercentageOfARoom()
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));
        var rate = room.AddRate("Standard", 79m);
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut, 1);

        var bookingRoom = booking.AddRoom(room, rate, 86.90m, vatPercentage: 10m);

        bookingRoom.VatPercentage.ShouldBe(10m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    public void NotAddARoomWithAVatPercentageOutOfRange(decimal vatPercentage)
    {
        var room = new Room("101", "Single Room", 1, new RoomType("Standard"));
        var rate = room.AddRate("Standard", 79m);
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut, 1);

        Action act = () => booking.AddRoom(room, rate, vatPercentage: vatPercentage);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void AddABillLine()
    {
        var booking = new Booking("20261006-0042", CreateGuest(), CheckIn, CheckOut, 1);

        var line = booking.AddBillLine("  Minibar ", 3, 6.50m, 22m);

        booking.BillLines.ShouldHaveSingleItem().ShouldBe(line);
        line.Description.ShouldBe("Minibar");
        line.Total.ShouldBe(19.50m);
    }

    [Fact]
    public void NotAddABillLineToACancelledBooking()
    {
        var (booking, a, b) = CreatePaidBookingWithTwoRooms();
        booking.CancelRooms([new RoomCancellation(a, 0m), new RoomCancellation(b, 0m)], CancelledAt, "Guest is ill");

        Action act = () => booking.AddBillLine("Minibar", 1, 5m, 22m);

        act.ShouldThrow<InvalidOperationException>();
        booking.BillLines.ShouldBeEmpty();
    }

    [Fact]
    public void AddABillLineToAPartlyCancelledBooking()
    {
        var (booking, _, b) = CreatePaidBookingWithTwoRooms();
        booking.CancelRooms([new RoomCancellation(b, 0m)], CancelledAt, "Guest is ill");

        var line = booking.AddBillLine("Minibar", 1, 5m, 22m);

        booking.BillLines.ShouldHaveSingleItem().ShouldBe(line);
    }

    /// <summary>
    /// a new checkout after a cancellation charges only the remaining rooms, the webhook compares the payment with this
    /// </summary>
    [Fact]
    public void LeaveCancelledRoomsOutOfTheActiveAmount()
    {
        var (booking, a, b) = CreatePaidBookingWithTwoRooms();
        booking.ActiveAmount.ShouldBe(200m);

        booking.CancelRooms([new RoomCancellation(b, 0m)], CancelledAt, "Guest is ill");

        booking.ActiveAmount.ShouldBe(a.TotalPrice);
    }
}

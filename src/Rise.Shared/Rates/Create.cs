namespace Rise.Shared.Rates;

public static partial class RateRequest
{
    public class Create
    {
        public required string Name { get; set; }
        public required decimal PricePerNight { get; set; }
        public string? Description { get; set; }
        public required string RoomName { get; set; }
    }
}

public static partial class RateResponse
{
    public class Create
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required decimal PricePerNight { get; set; }
        public string? Description { get; set; }
        public required string RoomName { get; set; }
    }
}
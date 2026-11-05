namespace Rise.Shared.ExtraServices;

/// <summary>
/// Extra service data shared between client and server.
/// </summary>
public static class ExtraServiceDto
{
    /// <summary>
    /// Service item displayed in admin and selection lists.
    /// </summary>
    public class Index
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required decimal Price { get; set; }
        public required ChargeCadence Cadence { get; set; }
        public required bool IsPerPerson { get; set; }
        public required bool IsActive { get; set; }
    }
}

namespace Rise.Shared.ExtraServices;

public static partial class ExtraServiceResponse
{
    /// <summary>
    /// Response containing the list of extra services.
    /// </summary>
    public class Index
    {
        public required IEnumerable<ExtraServiceDto.Index> Services { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Rise.Shared.Guests
{
        /// <summary>
        /// Represents the data transfer object (DTO) for guest information, including personal details and address.
        /// </summary>
        public class GuestInformationDTO
    {
        [Required]
        [MaxLength(250)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(250)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string AddressLine1 { get; set; } = string.Empty;

        [MaxLength(250)]
        public string AddressLine2 { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string Country { get; set; } = string.Empty;
    }
}
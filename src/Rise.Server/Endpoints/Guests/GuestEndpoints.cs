using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Rise.Shared.Guests;
using Rise.Domain.Guests;
using Rise.Persistence;
using System.Threading.Tasks;

namespace Rise.Server.Endpoints
{
    /// <summary>
    /// Provides extension methods to map guest-related endpoints to the API.
    /// </summary>
    public static class GuestEndpoints
    {
        public static void MapGuestEndpoints(this IEndpointRouteBuilder app) // Extension Method (of the Core-Class of .NET) withoud changing Sourcecode 
        {
            app.MapPost("/api/guests", async (GuestInformationDTO guestDto, ApplicationDbContext dbContext) => //Server listens to /api/guests and expects a POST request with a GuestInformationDTO in the body
            {
                var guestAdress = new GuestAddress( // creating a new guest objects via the data of the transport object
                    guestDto.AdressLine1,
                    guestDto.AdressLine2,
                    guestDto.Country
                );

                var fullName = $"{guestDto.FirstName} {guestDto.LastName}"; //combines the first and last name into a full name string via String interpolation and deletes accidental whitespace at the beginning and end of the string with Trim() method

                var newGuest = new Guest( // creates a new guest object with the data of the transport object and the newly created guestAdress and fullName objects
                    fullName,
                    guestDto.Email,
                    guestDto.PhoneNumber,
                    guestAdress
                );

                dbContext.Guests.Add(newGuest); // adds the new guest object to the Guests DbSet of the ApplicationDbContext, which represents the Guests table in the database
                await dbContext.SaveChangesAsync(); // translates the object into SQL and sends it to the database, saving the new guest record
                return Results.Ok();
            });
        }
    }
}


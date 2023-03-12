using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public record RegisterDto(
    [Required] string UserName,
    [Required] string KnownAs,
    [Required] string Gender,
    [Required] DateOnly? DateOfBirth,  // optional to make "Required" work...
    [Required] string City,
    [Required] string Country,
    [StringLength(8, MinimumLength = 4), Required] string Password
);

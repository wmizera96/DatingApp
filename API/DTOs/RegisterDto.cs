using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public record RegisterDto(
    [Required] string UserName,
    [StringLength(8, MinimumLength = 4), Required] string Password
);

using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public record LoginDto(
    [Required] string UserName,
    [Required] string Password
);
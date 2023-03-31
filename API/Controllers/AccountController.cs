using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController : BaseApiController
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public AccountController(UserManager<AppUser> userManager, ITokenService tokenService, IMapper mapper)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _mapper = mapper;
    }


    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto, CancellationToken cancellationToken)
    {
        if (await UserExists(registerDto.UserName, cancellationToken))
        {
            return BadRequest("Username is taken");
        }

        var user = _mapper.Map<AppUser>(registerDto);

        user.UserName = registerDto.UserName.ToLower();

        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var roleResult = await _userManager.AddToRoleAsync(user, "Member");

        if (!roleResult.Succeeded)
            return BadRequest(roleResult.Errors);
        
        return new UserDto(user.UserName, await _tokenService.CreateToken(user), user.Photos.FirstOrDefault(x => x.IsMain)?.Url, user.KnownAs, user.Gender);
    }


    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users
            .Include(x => x.Photos)
            .SingleOrDefaultAsync(x => x.UserName == loginDto.UserName, cancellationToken);

        if (user is null)
            return Unauthorized("Invalid user name");

        var result = await _userManager.CheckPasswordAsync(user, loginDto.Password);

        if (!result)
            return Unauthorized("Invalid password");
        
        return new UserDto(user.UserName, await _tokenService.CreateToken(user), user.Photos.FirstOrDefault(x => x.IsMain)?.Url, user.KnownAs, user.Gender);
    }



    private async Task<bool> UserExists(string userName, CancellationToken cancellationToken)
    {
        return await _userManager.Users.AnyAsync(x => x.UserName == userName.ToLower(), cancellationToken);
    }
}
using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController : BaseApiController
{
    private readonly DataContext _dataContext;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public AccountController(DataContext dataContext, ITokenService tokenService, IMapper mapper)
    {
        _tokenService = tokenService;
        _mapper = mapper;
        _dataContext = dataContext;
    }


    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto, CancellationToken cancellationToken)
    {
        if (await UserExists(registerDto.UserName, cancellationToken))
        {
            return BadRequest("Username is taken");
        }

        var user = _mapper.Map<AppUser>(registerDto);
        
        using var hmac = new HMACSHA512();

        user.UserName = registerDto.UserName.ToLower();
        user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
        user.PasswordSalt = hmac.Key;

        _dataContext.Add(user);
        await _dataContext.SaveChangesAsync(cancellationToken);

        return new UserDto(user.UserName, _tokenService.CreateToken(user), user.Photos.FirstOrDefault(x => x.IsMain)?.Url, user.KnownAs, user.Gender);
    }


    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto, CancellationToken cancellationToken)
    {
        var user = await _dataContext.Users.Include(x => x.Photos).SingleOrDefaultAsync(x => x.UserName == loginDto.UserName, cancellationToken);

        if (user is null)
            return Unauthorized("Invalid user name");

        using var hmac = new HMACSHA512(user.PasswordSalt);

        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

        for (int i = 0; i < computedHash.Length; i++)
        {
            if (computedHash[i] != user.PasswordHash[i])
                return Unauthorized("Invalid password");
        }

        return new UserDto(user.UserName, _tokenService.CreateToken(user), user.Photos.FirstOrDefault(x => x.IsMain)?.Url, user.KnownAs, user.Gender);
    }



    private async Task<bool> UserExists(string userName, CancellationToken cancellationToken)
    {
        return await _dataContext.Users.AnyAsync(x => x.UserName == userName.ToLower(), cancellationToken);
    }
}
﻿using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class LikesRepository : ILikesRepository
{
    private readonly DataContext _dataContext;

    public LikesRepository(DataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    public async Task<UserLike> GetUserLike(int sourceUserId, int targetUserId)
    {
        return await _dataContext.Likes.FindAsync(sourceUserId, targetUserId);
    }

    public async Task<PagedList<LikeDto>> GetUserLikes(LikesParams likesParams)
    {
        var users = _dataContext.Users.OrderBy(u => u.UserName).AsQueryable();
        var likes = _dataContext.Likes.AsQueryable();

        if (likesParams.Predicate == "liked")
        {
            likes = likes.Where(like => like.SourceUserId == likesParams.UserId);
            users = likes.Select(like => like.TargetUser);
        }
        
        if (likesParams.Predicate == "likedBy")
        {
            likes = likes.Where(like => like.TargetUserId == likesParams.UserId);
            users = likes.Select(like => like.SourceUser);
        }

        var likedUsers = users.Select(user => new LikeDto(user.Id, user.UserName, user.DateOfBirth.CalculateAge(),
            user.KnownAs, user.Photos.FirstOrDefault(photo => photo.IsMain).Url, user.City));

        return await PagedList<LikeDto>.CreateAsync(likedUsers, likesParams.PageNumber, likesParams.PageSize);
    }
    
    public async Task<AppUser> GetUserWithLikes(int userId)
    {
        return await _dataContext.Users.Include(x => x.LikedUsers).FirstOrDefaultAsync(x => x.Id == userId);
    }
}
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Helpers;

public class LogUserActivity : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var resultContext = await next();

        var user = resultContext.HttpContext.User;
        if (user.Identity is not {IsAuthenticated: true}) 
            return;

        var userId = user.GetUserId();
        var uow = resultContext.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
        var appUser = await uow.UserRepository.GetUserByIdAsync(userId);
        appUser.LastActive = DateTime.UtcNow;
        await uow.Complete();
    }
}
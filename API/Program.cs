using API.Data;
using API.Entities;
using API.Extensions;
using API.Middleware;
using API.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// dotnet ef database drop
// dontet ef database update
// docker build -t wmizera96/datingapp .
// docker run -rm -it -p 8080:80 wmizera96/datingapp:latest
// docker push wmizera96/datingapp:latest

// fly launch --image wmizera96/datingapp:latest
// fly secrets list
// fly secrets set my__secret=my-secret
// fly deploy

// password generator
// https://delinea.com/resources/password-generator-it-tool

builder.Services.AddControllers();


builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);


string connString;
if (builder.Environment.IsDevelopment())
{
    connString = builder.Configuration.GetConnectionString("postgres");
}
else
{
// Use connection string provided at runtime by Heroku.
    var connUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

    // Parse connection URL to connection string for Npgsql
    connUrl = connUrl.Replace("postgres://", string.Empty);
    var pgUserPass = connUrl.Split("@")[0];
    var pgHostPortDb = connUrl.Split("@")[1];
    var pgHostPort = pgHostPortDb.Split("/")[0];
    var pgDb = pgHostPortDb.Split("/")[1];
    var pgUser = pgUserPass.Split(":")[0];
    var pgPass = pgUserPass.Split(":")[1];
    var pgHost = pgHostPort.Split(":")[0];
    var pgPort = pgHostPort.Split(":")[1];
    var updatedHost = pgHost.Replace("flycast", "internal");

    connString = $"Server={updatedHost};Port={pgPort};User Id={pgUser};Password={pgPass};Database={pgDb};";
}

builder.Services.AddDbContext<DataContext>(opt =>
{
    // using SqlServer because of probelms with Sqlite
    // opt.UseSqlite(config.GetConnectionString("DefaultConnection"));
    // opt.UseSqlServer(config.GetConnectionString("SqlServer"));
    
    // dotnet ef migrations add PostgresInitial -o Data/Migrations
    opt.UseNpgsql(connString);
});


var app = builder.Build();

// has to be at the top
app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
app.UseCors(builder => builder
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials() // this is needed to allow SignalR to authenticate to the server
    .WithOrigins("https://localhost:4200"));

// need to be before MapControllers and after UseCors
app.UseAuthentication();
app.UseAuthorization();

// this has to be after authorization, before MapControllers
app.UseDefaultFiles(); // serves index.html
app.UseStaticFiles(); // serves other static files


app.MapControllers();
app.MapHub<PresenceHub>("hubs/presence");
app.MapHub<MessageHub>("hubs/message");
app.MapFallbackToController("Index", "Fallback");

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
try
{
    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
    var context = services.GetRequiredService<DataContext>();
    // creates and updates database on app start
    await context.Database.MigrateAsync();

    await Seed.ClearConnections(context);
    await Seed.SeedUsers(userManager, roleManager);
}
catch (Exception ex)
{
    var logger = services.GetService<ILogger<Program>>();
    logger.LogError(ex, "An error occured during a migration");
}

app.Run();
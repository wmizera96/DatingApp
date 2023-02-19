using API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// dotnet ef database drop
// dontet ef database update


builder.Services.AddControllers();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors(builder => builder.AllowAnyHeader().AllowAnyMethod().WithOrigins("https://localhost:4200"));

// need to be before MapControllers and after UseCors
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

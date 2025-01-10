using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<ValorantDbContext>(options =>
    options.UseInMemoryDatabase("ValorantDb").EnableSensitiveDataLogging());
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient<VlrScraperService>();
builder.Services.AddScoped<VlrScraperService>();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ValorantDbContext>();
    DbSeeder.Seed(context);
}


app.UseHttpsRedirection();
app.MapControllers();
app.Run();

using Microsoft.EntityFrameworkCore;
using HotelAPI.Data; // your DbContext namespace

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev", builder =>
    {
        builder.WithOrigins("http://localhost:5173") // React dev server
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});


// Configure PostgreSQL with connection string from appsettings.json or env variable
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Enable Swagger in development and production
app.UseSwagger();
app.UseSwaggerUI();

// Simple health-check/test endpoint
app.MapGet("/", () => "Hotel API is running ✅");

// Example: Quick test query to list hotels (using EF Core)
app.MapGet("/hotels", async (AppDbContext db) =>
    await db.HotelSales.ToListAsync());

// Map attribute-based controllers
app.MapControllers();

app.Run();

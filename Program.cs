using Microsoft.EntityFrameworkCore;
using HotelAPI.Data; // your DbContext namespace

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// Configure PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.MapControllers();

app.Run();

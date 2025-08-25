using Microsoft.EntityFrameworkCore;
using HotelAPI.Data; // your DbContext namespace

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev", builder =>
    {
        builder.WithOrigins("https://hotels-ui-obxn.onrender.com") // React dev server
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

// Configure PostgreSQL with connection string
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI();

// **Enable CORS middleware**
app.UseCors("AllowReactDev");

// Use routing
app.UseRouting();

// Map controllers
app.UseAuthorization();
app.MapControllers();

// Simple health-check/test endpoint
app.MapGet("/", () => "Hotel API is running ✅");

app.Run();

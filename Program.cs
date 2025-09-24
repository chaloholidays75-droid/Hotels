using Microsoft.EntityFrameworkCore;
using HotelAPI.Data;
using HotelAPI.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HotelAPI.Services;
// using HotelAPI.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program)); // Or specify a profile class

// Add services to the container
builder.Services.AddControllers(options =>
{
    // Apply ActivityLogFilter globally
    // options.Filters.Add<ActivityLogFilter>();
});
builder.Logging.ClearProviders();
builder.Logging.AddConsole();


// Register HttpContextAccessor for ActivityLogFilter
builder.Services.AddHttpContextAccessor();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.WithOrigins("https://backend.chaloholidayonline.com", "http://localhost:5173") // React dev server
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});
builder.Logging.ClearProviders();
builder.Logging.AddConsole();    // Logs will show in console
builder.Logging.AddDebug();      // Logs will show in debug output

// Configure PostgreSQL with connection string
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure app settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
// builder.Services.AddScoped<IActivityLoggerService, ActivityLoggerService>();
// builder.Services.AddScoped<ActivityLogFilter>(); // Filter depends on logger

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

var app = builder.Build();

// Development settings
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Shows full stack trace
}

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Enable routing
app.UseRouting();

// Enable CORS middleware
app.UseCors("AllowAll");

// Enable authentication & authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Simple health-check endpoint
app.MapGet("/", () => "Hotel API is running ✅");

app.Run();

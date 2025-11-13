using Microsoft.EntityFrameworkCore;
using HotelAPI.Data;
using HotelAPI.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HotelAPI.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------
// 🧭 Logging
// ------------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ------------------------------------------------------
// 🧩 Controllers + JSON options
// ------------------------------------------------------
builder.Services.AddControllers().AddJsonOptions(o =>
{
    // ✅ 1. Prevent circular reference serialization errors
    o.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    
    // ✅ 2. Match frontend JSON naming (guestName → GuestName)
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;

    // (Optional but recommended for consistency)
    o.JsonSerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// ------------------------------------------------------
// 🔗 Swagger / OpenAPI
// ------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ------------------------------------------------------
// 🛠 CORS (must allow credentials + both domains)
// ------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "https://backend.chaloholidayonline.com",
                "http://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // ✅ Needed for cookies
    });
});

// ------------------------------------------------------
// 🗄 PostgreSQL
// ------------------------------------------------------
NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddAutoMapper(typeof(Program));

// ------------------------------------------------------
// ⚙️ App Settings
// ------------------------------------------------------
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// ------------------------------------------------------
// 🧱 Services & Hosted Jobs
// ------------------------------------------------------
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IActivityLoggerService, ActivityLoggerService>();
builder.Services.AddHostedService<RememberTokenCleanupService>();
builder.Services.AddHttpContextAccessor();

// ------------------------------------------------------
// 🔐 JWT Authentication (reads token from cookie + header)
// ------------------------------------------------------
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            ),
            ClockSkew = TimeSpan.Zero
        };

        // ✅ Hybrid mode — allow cookie-based token too
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Use Authorization header first, else cookie
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) &&
                    context.Request.Cookies.TryGetValue("accessToken", out var cookieToken))
                {
                    context.Token = cookieToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// ------------------------------------------------------
// 🚀 Build & Configure Middleware Pipeline
// ------------------------------------------------------
var app = builder.Build();

// ------------------------------------------------------
// 💡 Development vs Production
// ------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

// ------------------------------------------------------
// 🧩 Swagger
// ------------------------------------------------------
app.UseSwagger();
app.UseSwaggerUI();

// ------------------------------------------------------
// 🌐 Middleware Order
// ------------------------------------------------------
app.UseRouting();
app.UseCors("AllowFrontend");     // ✅ Must be before auth for cookies
app.UseAuthentication();          // ✅ Reads JWT + cookies
app.UseAuthorization();
app.MapControllers();

// ------------------------------------------------------
// 🩺 Health Checks
// ------------------------------------------------------
app.MapGet("/", () => "Hotel API is running ✅");
app.MapGet("/api/test", () => "API works!");

// Optional endpoint listing for debugging
var routeEndpoints = app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>().Endpoints;
foreach (var endpoint in routeEndpoints)
{
    Console.WriteLine(endpoint.DisplayName);
}

app.Run();

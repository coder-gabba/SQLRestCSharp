using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using dotenv.net;
using System.Text;
using SqlAPI.Data;
using SqlAPI.Services;

// Load environment variables from .env file
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

// Register DbContext with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")));

// Register controllers and API services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "SQL API", 
        Version = "v1",
        Description = "A simple REST API demonstrating CRUD operations with JWT authentication"
    });
    
    // Include XML comments for better API documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Register custom services
builder.Services.AddSingleton<JwtService>();

// Configure JWT authentication
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") 
    ?? throw new InvalidOperationException("JWT_KEY environment variable is not configured");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
    ?? throw new InvalidOperationException("JWT_ISSUER environment variable is not configured");
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
    ?? throw new InvalidOperationException("JWT_AUDIENCE environment variable is not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Configure authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminPolicy", policy => 
        policy.RequireRole("Admin"));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SQL API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
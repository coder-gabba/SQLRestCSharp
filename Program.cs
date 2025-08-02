using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using dotenv.net;
using System.Text;
using SqlAPI.Data;
using SqlAPI.Services;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Reflection;

// Load environment variables from .env file
DotEnv.Load();

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/sqlapi-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Database initialization
await InitializeDatabaseAsync();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog to the host
    builder.Host.UseSerilog();

    // Configure services
    ConfigureServices(builder.Services);

    var app = builder.Build();

    // Configure pipeline
    ConfigurePipeline(app);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static async Task InitializeDatabaseAsync()
{
    var dbHandler = new PostgreSqlDatabaseHandler(Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")!);
    
    Log.Information("Initializing database schema...");
    await dbHandler.CreateTableAsync<SqlAPI.Models.Person>();
    await dbHandler.CreateTableAsync<SqlAPI.Models.User>();
    Log.Information("Database tables ready");

    // Development test data
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
    {
        Log.Information("Development mode - checking for test data");
        var existingPeople = await dbHandler.GetAllAsync<SqlAPI.Models.Person>();
        if (!existingPeople.Any())
        {
            Log.Information("No test data found - inserting sample person");
            await dbHandler.InsertAndCloseAsync(new SqlAPI.Models.Person
            {
                Name = "Max Mustermann",
                Age = 30,
                Email = "max@mustermann.de"
            });
            Log.Information("Sample data inserted");
        }
        else
        {
            Log.Information($"Found {existingPeople.Count} existing person records");
        }
    }

    Log.Information("Database initialization complete");
}

static void ConfigureServices(IServiceCollection services)
{
    // Register DbContext with PostgreSQL
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")));

    // Register AutoMapper
    services.AddAutoMapper(Assembly.GetExecutingAssembly());

    // Register controllers and add FluentValidation
    services.AddControllers();
    services.AddFluentValidationAutoValidation();
    services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    
    // API Documentation
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "SQL API",
            Version = "v1",
            Description = "A simple REST API demonstrating CRUD operations with JWT authentication"
        });

        // Include XML comments for better API documentation
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // Register custom services
    services.AddSingleton<JwtService>();
    services.AddSingleton(new PostgreSqlDatabaseHandler(Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")!));

    // Configure JWT authentication
    var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
        ?? throw new InvalidOperationException("JWT_KEY environment variable is not configured");
    var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
        ?? throw new InvalidOperationException("JWT_ISSUER environment variable is not configured");
    var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
        ?? throw new InvalidOperationException("JWT_AUDIENCE environment variable is not configured");

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
    services.AddAuthorizationBuilder()
        .AddPolicy("AdminPolicy", policy =>
            policy.RequireRole("Admin"));
}

static void ConfigurePipeline(WebApplication app)
{
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
}
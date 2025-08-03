using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using dotenv.net;
using System.Text;
using SqlAPI.Data;
using SqlAPI.Services;
using SqlAPI.Middleware;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Reflection;
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;

// Load environment variables from .env file
DotEnv.Load();

// Configure Serilog for structured logging from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
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

static async Task EnsureDatabaseExistsAsync(string connectionString, Microsoft.Extensions.Logging.ILogger logger)
{
    try
    {
        // Parse the connection string to extract database name and create a connection to 'postgres' system database
        var connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = connectionStringBuilder.Database;
        
        if (string.IsNullOrEmpty(databaseName))
        {
            Log.Warning("No database name found in connection string, assuming database exists");
            return;
        }

        // Create connection to the system 'postgres' database to check if our database exists
        connectionStringBuilder.Database = "postgres";
        var systemConnectionString = connectionStringBuilder.ToString();

        Log.Information("Checking if database '{DatabaseName}' exists...", databaseName);

        using var connection = new Npgsql.NpgsqlConnection(systemConnectionString);
        await connection.OpenAsync();

        // Check if database exists
        using var checkCommand = new Npgsql.NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @databaseName", connection);
        checkCommand.Parameters.AddWithValue("@databaseName", databaseName);

        var exists = await checkCommand.ExecuteScalarAsync();

        if (exists == null)
        {
            Log.Information("Database '{DatabaseName}' does not exist. Creating it...", databaseName);

            // Create the database
            using var createCommand = new Npgsql.NpgsqlCommand(
                $"CREATE DATABASE \"{databaseName}\"", connection);
            await createCommand.ExecuteNonQueryAsync();

            Log.Information("Database '{DatabaseName}' created successfully", databaseName);
        }
        else
        {
            Log.Information("Database '{DatabaseName}' already exists", databaseName);
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not ensure database exists - continuing with initialization");
    }
}

static async Task EnsureEntityFrameworkDatabaseAsync(string connectionString, Microsoft.Extensions.Logging.ILogger logger)
{
    try
    {
        Log.Information("Ensuring Entity Framework database schema is up to date...");
        
        // Create a temporary service collection for DbContext
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog());
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));
        
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Ensure database is created using Entity Framework
        var created = await dbContext.Database.EnsureCreatedAsync();
        if (created)
        {
            Log.Information("Entity Framework database schema created");
        }
        else
        {
            Log.Information("Entity Framework database schema already exists");
        }
        
        // Optional: Apply pending migrations if you're using migrations
        // var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        // if (pendingMigrations.Any())
        // {
        //     Log.Information("Applying {Count} pending database migrations", pendingMigrations.Count());
        //     await dbContext.Database.MigrateAsync();
        //     Log.Information("Database migrations applied successfully");
        // }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not ensure Entity Framework database - continuing with custom table creation");
    }
}

static async Task InitializeDatabaseAsync()
{
    var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")!;
    
    // Create a temporary logger for database initialization
    using var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
    var logger = loggerFactory.CreateLogger<PostgreSqlDatabaseHandler>();
    
    try
    {
        // First, ensure the database exists
        await EnsureDatabaseExistsAsync(connectionString, logger);
        
        // Initialize database using Entity Framework migrations
        await EnsureEntityFrameworkDatabaseAsync(connectionString, logger);
        
        var dbHandler = new PostgreSqlDatabaseHandler(connectionString, logger);
        
        Log.Information("Initializing database schema...");
        await CreateAllTablesAsync(dbHandler);
        Log.Information("Database tables ready");

        // Development test data
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            Log.Information("Development mode - checking for test data");
            try
            {
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

                // Add a test user if none exists
                var existingUsers = await dbHandler.GetAllAsync<SqlAPI.Models.User>();
                if (!existingUsers.Any())
                {
                    Log.Information("No test users found - inserting sample user");
                    await dbHandler.InsertAndCloseAsync(new SqlAPI.Models.User
                    {
                        Username = "admin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                        Role = "Admin"
                    });
                    Log.Information("Sample user inserted");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not insert test data - continuing without test data");
            }
        }

        Log.Information("Database initialization complete");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Database initialization failed - continuing without database");
    }
}

static async Task CreateAllTablesAsync(PostgreSqlDatabaseHandler dbHandler)
{
    // Create tables for all entity models
    // This approach automatically ensures all models are covered
    Log.Information("Creating tables for all entity models...");
    
    try
    {
        await dbHandler.CreateTableAsync<SqlAPI.Models.Person>();
        Log.Information("Person table ready");
        
        await dbHandler.CreateTableAsync<SqlAPI.Models.User>();
        Log.Information("User table ready");
        
        // Add more models here as your application grows
        // await dbHandler.CreateTableAsync<SqlAPI.Models.YourNewModel>();
        
        Log.Information("All database tables created successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error creating database tables");
        throw;
    }
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

    // Add Polly for resilience
    services.AddHttpClient("DefaultClient")
        .AddPolicyHandler(HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

    // Register custom services
    services.AddSingleton<JwtService>();
    services.AddSingleton(serviceProvider =>
    {
        var logger = serviceProvider.GetRequiredService<ILogger<PostgreSqlDatabaseHandler>>();
        return new PostgreSqlDatabaseHandler(Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")!, logger);
    });

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
    // Use the custom global exception handler
    app.UseMiddleware<GlobalExceptionHandler>();

    // Add Serilog's request logging
    app.UseSerilogRequestLogging();

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
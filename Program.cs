using ADDPerformance.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using ADDPerformance.Areas.Identity.Data;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// 1. Connection String
var connectionString = builder.Configuration.GetConnectionString("connectionString")
    ?? throw new InvalidOperationException("Connection string 'connectionString' not found.");

// 2. Contexts
builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContext<userContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Identity Configuration
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<userContext>()
.AddDefaultTokenProviders();

// Add Razor Pages so Areas/Identity UI pages are served
builder.Services.AddRazorPages();

// 4. JWT Configuration
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? "ReplaceThisWithAStrongKey_MustBeLongEnough";
var jwtIssuer = jwtSection["Issuer"] ?? "ADDPerformance";
var jwtAudience = jwtSection["Audience"] ?? "ADDPerformanceAudience";

// Register JWT as an additional authentication handler WITHOUT overwriting Identity's defaults
builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// 5. MVC and API Support with Global Authorization
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ADDPerformance API", Version = "v1" });
    c.CustomSchemaIds(type => (type.FullName ?? type.Name).Replace("+", "."));

    // Swagger JWT Security
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Enter your JWT Bearer token below."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// 6. Register Services
builder.Services.AddScoped<ADDPerformance.Services.IAddCkService, ADDPerformance.Services.AddCkService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("OpenCorsPolicy", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// 7. Auto-Migration & Seeding (Development Helper)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // Ensure migrations are applied before any Identity operations (avoid missing AspNetRoles table)
    var idContext = services.GetRequiredService<userContext>();
    try
    {
        // If there are pending migrations, apply them. If there are no migrations (e.g. none were created for Identity), fall back to EnsureCreated.
        var pending = idContext.Database.GetPendingMigrations();
        if (pending != null && pending.Any())
        {
            await idContext.Database.MigrateAsync();
            Console.WriteLine("Applied pending migrations for Identity (userContext).");
        }
        else
        {
            var created = await idContext.Database.EnsureCreatedAsync();
            if (created)
                Console.WriteLine("Identity database created using EnsureCreated().");
            else
                Console.WriteLine("No pending Identity migrations and database already exists (EnsureCreated returned false). Proceeding.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Identity migrations failed: {ex.GetType().Name} - {ex.Message}");
        // Try EnsureCreated as a last-resort fallback to create the Identity tables so seeding doesn't fail.
        try
        {
            var created = await idContext.Database.EnsureCreatedAsync();
            if (created)
                Console.WriteLine("Identity database created using EnsureCreated() fallback after migration failure.");
            else
                Console.WriteLine("EnsureCreated fallback did not create the Identity database (it may already exist).");
        }
        catch (Exception e2)
        {
            Console.WriteLine($"EnsureCreated fallback failed: {e2.GetType().Name} - {e2.Message}");
        }
    }

    try
    {
        var appContext = services.GetRequiredService<DBContext>();
        await appContext.Database.MigrateAsync();
        Console.WriteLine("Applied pending migrations for Application DB (DBContext).");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Application DB migrations failed: {ex.Message}");
    }

    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    try
    {
        // 1. Ensure Role Exists
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!roleResult.Succeeded)
            {
                Console.WriteLine($"Failed to create Admin role: {string.Join(',', roleResult.Errors.Select(e=>e.Description))}");
            }
        }

        // 2. Ensure Admin User Exists
        var adminEmail = "admin@local";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, "P@ssword1!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine("Successfully seeded admin@local user.");
            }
            else
            {
                Console.WriteLine($"Failed to create admin user: {string.Join(',', result.Errors.Select(e=>e.Description))}");
            }
        }
    }
    catch (Exception ex)
    {
        // Log full exception including stack trace and inner exception
        Console.WriteLine("Seeding failed: " + ex.ToString());
    }

    if (!await idContext.Database.CanConnectAsync())
    {
        Console.WriteLine("ERROR: Cannot connect to Identity database. ConnectionString=" + builder.Configuration.GetConnectionString("connectionString"));
    }
}

// 8. Pipeline
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

app.UseHttpsRedirection();
app.UseDefaultFiles(); 
app.UseStaticFiles();
app.UseRouting();
app.UseCors("OpenCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

// 9. Auth Endpoint for the PowerShell Test Script
app.MapPost("/api/auth/login", async (UserManager<IdentityUser> userManager, IConfiguration config, LoginRequest req) =>
{
    var user = await userManager.FindByEmailAsync(req.Email);
    if (user == null || !await userManager.CheckPasswordAsync(user, req.Password)) 
        return Results.Unauthorized();
    var jwtKey = config["Jwt:Key"];
    var jwtIssuer = config["Jwt:Issuer"] ?? "ADDPerformance";
    var jwtAudience = config["Jwt:Audience"] ?? "ADDPerformanceAudience";

    if (string.IsNullOrEmpty(jwtKey))
    {
        return Results.Problem("JWT Key is not configured in appsettings.json");
    }
    var key = Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? "u5nf144fmni5hfmforutjtb8f1tu9ogn");
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Email, user.Email!)
    };

    var roles = await userManager.GetRolesAsync(user);
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddHours(8),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
        Issuer = config["Jwt:Issuer"] ?? "ADDPerformance",
        Audience = config["Jwt:Audience"] ?? "ADDPerformanceAudience"
    };

    var tokenHandler = new JwtSecurityTokenHandler();
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Results.Ok(new { token = tokenHandler.WriteToken(token) });
})
.AllowAnonymous();

// 10. Swagger Routes
app.MapGet("/swagger/v1/swagger.json", () => Results.Redirect("/swagger/v1/swagger.json"));
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ADDPerformance API V1");
    c.RoutePrefix = "swagger";
});

// Ensure you map Razor Pages so the Identity UI is available
app.MapRazorPages();

app.MapControllers();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Request Model
record LoginRequest(string Email, string Password);
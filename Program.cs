using ADDPerformance.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// 1. Connection String
var connectionString = builder.Configuration.GetConnectionString("connectionString")
    ?? throw new InvalidOperationException("Connection string 'connectionString' not found.");

// 2. Add Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 3. Configure Swagger (Swashbuckle)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ADDPerformance API", Version = "v1" });
});

// 4. Database Context
builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlServer(connectionString));

// 5. CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("OpenCorsPolicy", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// 6. Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ADDPerformance API v1");
        // This makes the Swagger UI the default page when you open the root URL
        c.RoutePrefix = "swagger";
    });
}
app.MapGet("/", () => Results.Redirect("/swagger"));
// Serve static files early
// Serve static files and HTTPS first
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Serve Swagger in all environments and before auth so swagger.json is public
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ADDPerformance API v1");
    c.RoutePrefix = "swagger";
});

// CORS
app.UseCors("OpenCorsPolicy");

// Authentication/Authorization for API endpoints only
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
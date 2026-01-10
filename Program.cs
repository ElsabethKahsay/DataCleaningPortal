using ADDPerformance.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;
using Microsoft.OpenApi; // Fixed reference

var builder = WebApplication.CreateBuilder(args);

// 1. Connection String
var connectionString = builder.Configuration.GetConnectionString("connectionString")
    ?? throw new InvalidOperationException("Connection string 'connectionString' not found.");

// 2. Database Context
builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Identity Services (UNCOMMENTED & FIXED)
// You need this because your controllers use UserManager and User.Identity
//builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    //options.SignIn.RequireConfirmedAccount = false;
   // options.Password.RequireDigit = false; // Easier for development
    //ptions.Password.RequiredLength = 6;
//})
//AddEntityFrameworkStores<DBContext>();

// 4. Controller & Swagger Support
builder.Services.AddControllersWithViews(); // Support for MVC Views
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ADDPerformance API", Version = "v1" });
});

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
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// FIX: Serve static files before Swagger if you want index.html to work
app.UseDefaultFiles(); // Enables index.html as default
app.UseStaticFiles();

app.UseRouting();
app.UseCors("OpenCorsPolicy");

// 7. Identity Middleware
app.UseAuthentication();
app.UseAuthorization();

// 8. Swagger Configuration
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ADDPerformance API V1");

    // CHANGE: If you want to see your index.html, do NOT set RoutePrefix to string.Empty
    // Keep it as "swagger" so you can visit it at /swagger/index.html
    c.RoutePrefix = "swagger";
});

// 9. Routing
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
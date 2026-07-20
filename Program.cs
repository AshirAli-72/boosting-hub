using System.Text;
using BoostingHub.backend.Data;
using BoostingHub.backend.Data.Seeders;
using BoostingHub.backend.Services.Implementations;
using BoostingHub.backend.Services.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(3);
            sqlOptions.CommandTimeout(60);
        }));

builder.Services.AddScoped<ApplicationDbContext>(p =>
    p.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
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
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});

// Authorization
builder.Services.AddAuthorization();

// MVC + Razor Pages + API Controllers
builder.Services.AddControllers();
builder.Services.AddRazorPages(options =>
{
    options.RootDirectory = "/frontend/Pages";
});
builder.Services.AddHttpContextAccessor();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<Program>());

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Blazor Server
builder.Services.AddServerSideBlazor();

// SignalR
builder.Services.AddSignalR();

// Services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IProofVerificationService, ProofVerificationService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddHttpClient("ProofVerification", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ProofVerification/1.0");
});

var app = builder.Build();

// Seed data
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        await AdminSeeder.SeedAsync(db);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[Startup] Database seeding failed: {ex.Message}");
}

    var env = builder.Environment.EnvironmentName;
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
    var dbLabel = connStr.Contains("localhost") || connStr.Contains("SQLEXPRESS") || connStr.Contains("Trusted_Connection")
        ? "LOCAL (SQL Express)" : "PRODUCTION (Somee)";
    Console.WriteLine($"[Startup] Environment: {env} | Database: {dbLabel}");

    app.UseDeveloperExceptionPage();
    app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllers();
app.MapRazorPages();
app.MapBlazorHub();
app.MapHub<BoostingHub.backend.Hubs.TaskHub>("/hub/tasks");

app.Run();

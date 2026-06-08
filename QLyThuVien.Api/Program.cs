using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;
using QLyThuVien.Api.Hubs;
using QLyThuVien.Api.Middleware;
using QLyThuVien.Application.DependencyInjection;
using QLyThuVien.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "QLyThuVien";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "QLyThuVien.Api";
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "ql-thu-vien-demo-jwt-secret-key-for-development-2026";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseMiddleware<TenantContextMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SecurityHub>("/hubs/security");
app.MapHub<AdminHub>("/hubs/admin");
app.MapHub<CatalogHub>("/hubs/catalog");
app.MapHub<CirculationHub>("/hubs/circulation");
app.MapHub<NotificationHub>("/hubs/notification");
app.MapHub<InventoryHub>("/hubs/inventory");
app.MapHub<DashboardHub>("/hubs/dashboard");
app.MapHub<ReportHub>("/hubs/report");
app.MapHub<AiHub>("/hubs/ai");
app.MapFallbackToFile("index.html");

app.Run();

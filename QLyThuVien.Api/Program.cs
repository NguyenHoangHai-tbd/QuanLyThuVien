using System.Text.Json.Serialization;
using QLyThuVien.Api.Hubs;
using QLyThuVien.Api.Middleware;
using QLyThuVien.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();
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

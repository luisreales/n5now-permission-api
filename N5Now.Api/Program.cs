using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using N5Now.Infrastructure.Data;
using N5Now.Infrastructure.Handlers.RequestPermission;
using N5Now.Infrastructure.Kafka;
using Nest;


var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("serilog.json", optional: false)
    .Build();

// Configura Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting up the application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Add services
    builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Permissions API", Version = "v1" });
    });

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite("Data Source=/app/data/app.db"));

    builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(AppDbContext).Assembly));

    builder.Services.AddSingleton<IElasticClient>(sp =>
    {
        var settings = new ConnectionSettings(new Uri("http://elasticsearch:9200"))
            .DefaultIndex("permissions");
        return new ElasticClient(settings);
    });

    builder.Services.AddScoped<PermissionElasticService>();

    var app = builder.Build();

    // Apply DB migrations
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    // Middlewares
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "The application failed to start");
}
finally
{
    Log.CloseAndFlush();
}

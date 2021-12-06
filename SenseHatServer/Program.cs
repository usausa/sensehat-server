using Microsoft.Extensions.Options;

using SenseHatServer.Services;

using Serilog;

#pragma warning disable CA1812

//--------------------------------------------------------------------------------
// Configure builder
//--------------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

// Log
builder.Host
    .ConfigureLogging((_, logging) =>
    {
        logging.ClearProviders();
    })
    .UseSerilog((hostingContext, loggerConfiguration) =>
    {
        loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
    });

// Route
builder.Services.Configure<RouteOptions>(options =>
{
    options.AppendTrailingSlash = true;
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Storage
builder.Services.Configure<StorageServiceOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddSingleton(p => p.GetRequiredService<IOptions<StorageServiceOptions>>().Value);
builder.Services.AddSingleton<StorageService>();

// SenseHat
builder.Services.Configure<SenseHatServiceOptions>(builder.Configuration.GetSection("SenseHat"));
builder.Services.AddSingleton(p => p.GetRequiredService<IOptions<SenseHatServiceOptions>>().Value);
builder.Services.AddSingleton<SenseHatService>();

//--------------------------------------------------------------------------------
// Configure the HTTP request pipeline
//--------------------------------------------------------------------------------

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// TODO ignore api
app.MapGet("/", () => "Sense Hat API");
// TODO parameter
app.MapPost("/play", () =>
{
});
app.MapPost("/cancel", () =>
{
});
app.MapPost("/clear", () =>
{
});

app.Run();

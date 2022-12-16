using System.Globalization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using SenseHatServer.Devices;
using SenseHatServer.Services;

#pragma warning disable CA1852

//--------------------------------------------------------------------------------
// Configure builder
//--------------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

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

// Map
app.MapGet("/", () => "Sense Hat API").ExcludeFromDescription();

app.MapPost("play/{file}", async ([FromRoute] string file, [FromServices] StorageService storage, [FromServices] SenseHatService senseHat) =>
{
    await using var stream = await storage.ReadAsync(file);
    if (stream is null)
    {
        return Results.NotFound();
    }

    var movie = await SenseHatMovie.LoadAsync(stream);
    senseHat.Play(movie);

    return Results.Ok();
});

app.MapPost("/cancel", ([FromServices] SenseHatService senseHat) =>
{
    senseHat.Cancel();

    return Results.Ok();
});

app.MapPost("/clear", ([FromServices] SenseHatService senseHat) =>
{
    senseHat.Clear();

    return Results.Ok();
});

app.MapPost("/show", ([FromBody] string[][] values, [FromServices] SenseHatService senseHat) =>
{
    var height = (byte)values.Length;
    var width = (byte)values.Max(x => x.Length);
    var image = new SenseHatImage(width, height);
    for (byte y = 0; y < height; y++)
    {
        for (byte x = 0; x < width; x++)
        {
            if (Int32.TryParse(values[y][x].TrimStart('#'), NumberStyles.HexNumber, null, out var rgb))
            {
                image.SetPixel(x, y, new SenseHatColor((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF)));
            }
        }
    }

    senseHat.Show(image);

    return Results.Ok();
});

app.Run();

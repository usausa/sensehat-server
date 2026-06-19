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
builder.Services.Configure<RouteOptions>(static options =>
{
    options.AppendTrailingSlash = true;
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Storage
builder.Services.Configure<StorageServiceOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<StorageServiceOptions>>().Value);
builder.Services.AddSingleton<StorageService>();

// SenseHat
builder.Services.Configure<SenseHatServiceOptions>(builder.Configuration.GetSection("SenseHat"));
builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<SenseHatServiceOptions>>().Value);
builder.Services.AddSingleton<SenseHatService>();

//--------------------------------------------------------------------------------
// Configure the HTTP request pipeline
//--------------------------------------------------------------------------------

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Map
app.MapGet("/", static () => "Sense Hat API").ExcludeFromDescription();

app.MapPost("play/{file}", static async ([FromRoute] string file, [FromServices] StorageService storage, [FromServices] SenseHatService senseHat) =>
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

app.MapPost("/cancel", static ([FromServices] SenseHatService senseHat) =>
{
    senseHat.Cancel();

    return Results.Ok();
});

app.MapPost("/clear", static ([FromServices] SenseHatService senseHat) =>
{
    senseHat.Clear();

    return Results.Ok();
});

app.MapPost("/show", static ([FromBody] string?[]?[]? values, [FromServices] SenseHatService senseHat) =>
{
    if ((values is null) || (values.Length == 0))
    {
        return Results.BadRequest();
    }

    var image = new SenseHatImage(senseHat.Width, senseHat.Height);
    var height = Math.Min(values.Length, senseHat.Height);
    for (var y = 0; y < height; y++)
    {
        var row = values[y];
        if (row is null)
        {
            continue;
        }

        var width = Math.Min(row.Length, senseHat.Width);
        for (var x = 0; x < width; x++)
        {
            var value = row[x];
            if ((value is not null) && Int32.TryParse(value.TrimStart('#'), NumberStyles.HexNumber, null, out var rgb))
            {
                image.SetPixel((byte)x, (byte)y, new SenseHatColor((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF)));
            }
        }
    }

    senseHat.Show(image);

    return Results.Ok();
});

app.Run();

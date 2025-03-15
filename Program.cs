using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// In-memory хранилище рекламных площадок
ConcurrentDictionary<string, List<string>> adPlatforms = new();

// Метод для загрузки данных из файла
async Task UploadData(HttpContext context)
{
    using var reader = new StreamReader(context.Request.Body);
    string content = await reader.ReadToEndAsync();
    
    adPlatforms.Clear();
    foreach (var line in content.Split('\n'))
    {
        var parts = line.Split(":");
        if (parts.Length != 2) continue;

        var platform = parts[0].Trim();
        var locations = parts[1].Split(",").Select(loc => loc.Trim()).ToList();

        foreach (var location in locations)
        {
            adPlatforms.AddOrUpdate(location, new List<string> { platform }, (key, list) => { list.Add(platform); return list; });
        }
    }
    
    await context.Response.WriteAsync("Data uploaded successfully");
}

// Метод для поиска рекламных площадок по локации
IResult SearchPlatforms(HttpContext context)
{
    var location = context.Request.Query["location"].ToString();
    if (string.IsNullOrEmpty(location)) return Results.BadRequest("Location parameter is required");

    var result = adPlatforms
        .Where(kvp => location.StartsWith(kvp.Key))
        .SelectMany(kvp => kvp.Value)
        .Distinct()
        .ToList();
    
    return Results.Ok(result);
}

// Маршруты API
app.MapPost("/upload", UploadData);
app.MapGet("/search", SearchPlatforms);


// Маршруты API
app.MapPost("/upload", async ([FromBody] string[] lines) => await UploadData(lines));
app.MapGet("/search", ([FromQuery] string location) => SearchPlatforms(location));

app.Run();

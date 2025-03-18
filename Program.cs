using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы логирования
builder.Services.AddLogging(logging => 
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});

var app = builder.Build();

// Включаем логирование всех запросов
app.Use(async (context, next) =>
{
    var logger = app.Logger;
    logger.LogInformation($"Request: {context.Request.Method} {context.Request.Path}");
    await next();
});

// Инициализация структур данных
var locationTrie = new Trie();
var adPlatformTrie = new AdPlatformTrie();

// Загрузка данных для локаций
app.MapPost("/upload/locations", async ([FromBody] string filePath) =>
{
    if (!File.Exists(filePath)) 
        return Results.BadRequest("Файл не найден");
    
    locationTrie = new Trie();
    var lines = await File.ReadAllLinesAsync(filePath);
    foreach (var line in lines)
    {
        var parts = line.Split(',');
        if (parts.Length == 2)
        {
            locationTrie.Insert(parts[0].Trim(), parts[1].Trim());
        }
    }
    return Results.Ok("Данные локаций загружены успешно");
});

// Загрузка данных для рекламных платформ
app.MapPost("/upload/platforms", async (HttpContext context) =>
{
    var lines = await context.Request.ReadFromJsonAsync<List<string>>();
    if (lines == null || !lines.Any()) 
        return Results.BadRequest("Некорректные данные");

    foreach (var line in lines)
    {
        var parts = line.Split(':');
        if (parts.Length != 2) continue;

        var platform = parts[0].Trim();
        var locations = parts[1].Split(',').Select(loc => loc.Trim());
        
        foreach (var location in locations)
        {
            adPlatformTrie.AddPlatform(platform, location);
        }
    }
    return Results.Ok("Данные платформ загружены успешно");
});


app.MapGet("/search/locations/{location}", (ILogger<Program> logger, string location) =>
{
    var results = locationTrie.Search(location);
    logger.LogInformation($"LOCATIONS SEARCH: {location} => {results.Count} results");
    return results.Any() ? Results.Ok(results) : Results.NotFound();
});

app.MapGet("/search/platforms/{location}", (ILogger<Program> logger, string location) =>
{
    var results = adPlatformTrie.Search(location);
    logger.LogInformation($"PLATFORMS SEARCH: {location} => {string.Join(", ", results)}");
    return results.Any() ? Results.Ok(results) : Results.NotFound();
});

app.Run();
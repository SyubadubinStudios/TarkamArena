using TarkamArena.Server;

// Tarkam Arena online services.
//   dotnet run --project src/TarkamArena.Server            (SQLite file, port 5080)
//   ConnectionStrings__Postgres="Host=...;Database=tarkam;Username=...;Password=..." dotnet run ...
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://0.0.0.0:5080");

var postgres = builder.Configuration.GetConnectionString("Postgres");
IArenaStore store = string.IsNullOrWhiteSpace(postgres)
    ? SqlArenaStore.Sqlite(builder.Configuration["SqlitePath"] ?? Path.Combine(AppContext.BaseDirectory, "tarkam-server.db"))
    : SqlArenaStore.Postgres(postgres);
builder.Services.AddSingleton(store);
builder.Services.AddSingleton<Matchmaker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<Matchmaker>());
builder.Services.AddSignalR();

var app = builder.Build();
await store.InitializeAsync();

app.MapHub<ArenaHub>("/hubs/arena");
app.MapGet("/", () => Results.Text("Tarkam Arena server — made by Ariana Mischa Fadhila of Syubadubin Studios"));
app.MapGet("/health", (Matchmaker m) => Results.Ok(new { status = "ok", queue = m.QueueLength, database = string.IsNullOrWhiteSpace(postgres) ? "sqlite" : "postgres" }));
app.MapGet("/api/leaderboard", async (IArenaStore s, int? count) => Results.Ok(await s.TopPlayersAsync(Math.Clamp(count ?? 20, 1, 100))));
app.MapGet("/api/clans", async (IArenaStore s) => Results.Ok(await s.ClanStandingsAsync()));

app.Run();

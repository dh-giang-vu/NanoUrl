using Azure.Identity;
using NanoUrl.Models;
using NanoUrl.Services;

var builder = WebApplication.CreateBuilder(args);

var keyVaultEndpoint = new Uri(Environment.GetEnvironmentVariable("VaultUri"));
builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());

// Add services to the container.

builder.Services.Configure<DatabaseSettings>(options =>
{
    options.ConnectionString = builder.Configuration["MongoConnectionString"];
    options.DatabaseName = "NanoURL";
    options.UrlMapCollectionName = "UrlMapCollection";
});

builder.Services.AddSingleton<UrlMapService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Environment.GetEnvironmentVariable("WebUri") ?? "Missing Environment Variable \"WebUri\"");

app.Run();

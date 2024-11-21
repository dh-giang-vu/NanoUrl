using NanoUrl.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace NanoUrl.Services;

public class UrlMapService
{
    private readonly IMongoCollection<UrlMap> _urlMapsCollection;

    public UrlMapService(IOptions<DatabaseSettings> databaseSettings)
    {
        MongoClient client = new MongoClient(databaseSettings.Value.ConnectionString);
        var db = client.GetDatabase(databaseSettings.Value.DatabaseName);
        _urlMapsCollection = db.GetCollection<UrlMap>(databaseSettings.Value.UrlMapCollectionName);
    }

    public async Task<UrlMap> GetByShortCodeAsync(string shortCode) =>
        await _urlMapsCollection.Find(map => map.shortCode == shortCode).FirstOrDefaultAsync();

    public async Task<UrlMap> GetByOriginalAsync(string original) =>
        await _urlMapsCollection.Find(map => map.original == original).FirstOrDefaultAsync();

    public async Task CreateAsync(UrlMap urlMap) =>
        await _urlMapsCollection.InsertOneAsync(urlMap);

}
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NanoUrl.Models;

public class UrlMap
{
    [BsonElement]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string original { get; set; } = null!;
    public string shortCode { get; set; } = null!;
}
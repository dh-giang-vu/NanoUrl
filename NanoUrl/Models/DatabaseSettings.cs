namespace NanoUrl.Models;

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string UrlMapCollectionName { get; set; } = null!;

}
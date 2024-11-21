using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NanoUrl.Models;
using NanoUrl.Services;
using System.Text;

namespace nano_url.Controllers;

[Route("/")]
[ApiController]
public class UrlController : ControllerBase
{
    private readonly UrlMapService _urlMapService;

    public UrlController (UrlMapService urlMapService)
    {
        _urlMapService = urlMapService;
    }

    [HttpGet("{shortCode}")]
    public async Task<IActionResult> RedirectToUrl(string shortCode)
    {
        // Fetch the original URL
        var urlMapping = await _urlMapService.GetByShortCodeAsync(shortCode);
        if (urlMapping == null)
        {
            return NotFound("Website URL not found");
        }

        return Redirect(urlMapping.original);
    }

    [HttpPost("shorten")]
    public async Task<IActionResult> Post(string url)
    {
        // Check if URL is well-formed
        bool isUrl = Uri.IsWellFormedUriString(url, UriKind.Absolute);
        if (!isUrl)
        {
            return BadRequest("Not well-formed URL.");
        }

        string shortUrl = Environment.GetEnvironmentVariable("WebUri") ?? "";

        // Check if there exists a short URL for current input URL
        UrlMap existingMapping = await _urlMapService.GetByOriginalAsync(url);
        if (existingMapping != null)
        {
            return Ok($"{shortUrl}/{existingMapping.shortCode}");
        }

        // Generate new short URL for current input and store in database
        var guid = Guid.NewGuid().ToString();
        var hashCode = guid.GetHashCode();
        var shortCode = Base62Encode(hashCode);

        // Save new URL mapping to database
        UrlMap urlMap = new UrlMap { original = url, shortCode = shortCode };
        await _urlMapService.CreateAsync(urlMap);

        return Ok($"{shortUrl}/{shortCode}");
    }

    private static string Base62Encode(int number)
    {
        const string chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        int abs = Math.Abs(number);
        var result = new StringBuilder();
        int index = 0;
        while (abs > 0)
        {
            result.Insert(index, chars[(int)(abs % 62)]);
            abs /= 62;
            index++;
        }
        return result.ToString();
    }
}
using Microsoft.AspNetCore.Mvc;
using NanoUrl.Dto;
using NanoUrl.Models;
using NanoUrl.Services;
using System.Text;

namespace nano_url.Controllers;

[Route("/")]
[ApiController]
public class UrlController : ControllerBase
{
    private readonly UrlMapService _urlMapService;

    public UrlController(UrlMapService urlMapService)
    {
        _urlMapService = urlMapService;
    }

    [HttpGet("{shortCode}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404, Type = typeof(ErrorResponse))]
    [ProducesResponseType(500, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> RedirectToUrl(string shortCode)
    {
        // Fetch the original URL
        var urlMapping = await _urlMapService.GetByShortCodeAsync(shortCode);
        if (urlMapping == null)
        {
            return NotFound(new ErrorResponse {
                Message = "Short URL not found.",
                Detail = $"{Environment.GetEnvironmentVariable("WebUri")}/{shortCode}"
            });
        }

        // Check if the request originates from Swagger using the Referer header
        var referer = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer) && referer.Contains("swagger", StringComparison.OrdinalIgnoreCase))
        {
            return await ProxyRequest(urlMapping.original);
        }

        // Redirect for all other clients
        return Redirect(urlMapping.original);
    }

    [HttpGet("/get/{shortCode}")]
    [ProducesResponseType(200, Type = typeof(UrlMap))]
    [ProducesResponseType(404, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetOriginalUrl(string shortCode)
    {
        // Fetch the original URL
        var urlMapping = await _urlMapService.GetByShortCodeAsync(shortCode);
        if (urlMapping == null)
        {
            return NotFound(new ErrorResponse
            {
                Message = "Short URL not found.",
                Detail = $"{Environment.GetEnvironmentVariable("WebUri")}/{shortCode}"
            });
        }

        return Ok(urlMapping);
    }

    [HttpPost("custom")]
    [ProducesResponseType(200, Type = typeof(string))]
    [ProducesResponseType(400, Type = typeof(ErrorResponse))]
    [ProducesResponseType(500, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> Post([FromBody] UrlMap map)
    {
        string url = map.original;
        string shortCode = map.shortCode;

        // Check if URL is well-formed
        bool isUrl = Uri.IsWellFormedUriString(url, UriKind.Absolute);
        if (!isUrl)
        {
            return BadRequest(new ErrorResponse {
                Message = "URL is not well-formed",
                Detail = url
            });
        }

        string shortUrlPrefix = Environment.GetEnvironmentVariable("WebUri") ?? "";

        // Check if short code exists in database
        UrlMap existingMapping = await _urlMapService.GetByShortCodeAsync(shortCode);
        if (existingMapping != null)
        {
            return StatusCode(500, new
            {
                Message = "The short URL already exists and cannot be used.",
                Detail = $"{shortUrlPrefix}/{shortCode}"
            });
        }

        // Save new URL mapping to database
        UrlMap urlMap = new UrlMap { original = url, shortCode = shortCode };
        await _urlMapService.CreateAsync(urlMap);

        return Ok($"{shortUrlPrefix}/{shortCode}");
    }

    [HttpPost("shorten")]
    [ProducesResponseType(200, Type = typeof(string))]
    [ProducesResponseType(400, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> Post([FromBody] string url)
    {
        // Check if URL is well-formed
        bool isUrl = Uri.IsWellFormedUriString(url, UriKind.Absolute);
        if (!isUrl)
        {
            return BadRequest(new ErrorResponse {
                Message = "Not well-formed URL.",
                Detail = url
            });
        }

        string shortUrlPrefix = Environment.GetEnvironmentVariable("WebUri") ?? "";

        // Check if there exists a short URL for current input URL
        UrlMap existingMapping = await _urlMapService.GetByOriginalAsync(url);
        if (existingMapping != null)
        {
            return Ok($"{shortUrlPrefix}/{existingMapping.shortCode}");
        }

        // Generate new short URL for current input and store in database
        var guid = Guid.NewGuid().ToString();
        var hashCode = guid.GetHashCode();
        var shortCode = Base62Encode(hashCode);

        // Save new URL mapping to database
        UrlMap urlMap = new UrlMap { original = url, shortCode = shortCode };
        await _urlMapService.CreateAsync(urlMap);

        return Ok($"{shortUrlPrefix}/{shortCode}");
    }

    /**
     * Encode a number to Base62
     */
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

    /**
     * Proxies the request to the original URL and returns the response.
     * Helper function so that Swagger can bypass CORS restriction to display redirect result.
     */
    private async Task<IActionResult> ProxyRequest(string originalUrl)
    {
        using var httpClient = new HttpClient();
        try
        {
            var response = await httpClient.GetAsync(originalUrl);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new ErrorResponse
                {
                    Message = "Failed to fetch the original URL.",
                    Detail = originalUrl
                });
            }

            // Read the content and forward it to the client
            var content = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

            return File(content, contentType);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while fetching the original URL.",
                Detail = ex.Message
            });
        }
    }
}
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
    [ProducesResponseType(302)]
    [ProducesResponseType(404, Type = typeof(ErrorResponse))]
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
    public async Task<IActionResult> Post(string url, string shortCode)
    {
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
    public async Task<IActionResult> Post(string url)
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
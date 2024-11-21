namespace NanoUrl.Dto;

public class ErrorResponse
{
    public required string Message { get; set; }
    public string? Detail { get; set; }
}
namespace Api.Contracts;

public sealed record IngestJobErrorResponse
{
    public required string Code { get; init; }

    public required string Message { get; init; }
}

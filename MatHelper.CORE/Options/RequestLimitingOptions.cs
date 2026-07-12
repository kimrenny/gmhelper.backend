namespace MatHelper.CORE.Options;

public sealed class RequestLimitingOptions
{
    public RequestLimitRule Default { get; set; } = new();

    public Dictionary<string, RequestLimitRule> Endpoints { get; set; } = new();
}
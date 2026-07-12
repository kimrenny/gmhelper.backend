namespace MatHelper.CORE.Options;

public sealed class RequestLimitRule
{
    public int MaxConcurrentPerIp { get; set; }

    public int MaxConcurrentApplication { get; set; }

    public int MaxConcurrentEndpoint { get; set; }
}
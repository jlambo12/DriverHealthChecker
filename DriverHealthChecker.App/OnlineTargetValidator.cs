namespace DriverHealthChecker.App;

internal interface IOnlineTargetValidator
{
    bool IsValidUrl(string url);
}

internal sealed class OnlineTargetValidator : IOnlineTargetValidator
{
    public bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme is "http" or "https";
    }
}

using System.IO;

namespace DriverHealthChecker.App;

internal interface ILocalAppValidator
{
    bool Exists(string path);
}

internal sealed class LocalAppValidator : ILocalAppValidator
{
    public bool Exists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        return File.Exists(path);
    }
}

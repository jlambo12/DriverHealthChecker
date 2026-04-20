namespace DriverHealthChecker.App;

internal interface IOfficialSupportChannelResolver
{
    OfficialSupportChannel Resolve(DriverIdentity identity);
}

internal sealed class OfficialSupportChannelResolver : IOfficialSupportChannelResolver
{
    private readonly NvidiaSupportResolver _nvidiaSupportResolver;
    private readonly IntelSupportResolver _intelSupportResolver;
    private readonly AmdSupportResolver _amdSupportResolver;
    private readonly GenericSupportResolver _genericSupportResolver;

    public OfficialSupportChannelResolver()
    {
        var installedOfficialAppCatalog = new InstalledOfficialAppCatalog();
        _nvidiaSupportResolver = new NvidiaSupportResolver(installedOfficialAppCatalog);
        _intelSupportResolver = new IntelSupportResolver(installedOfficialAppCatalog);
        _amdSupportResolver = new AmdSupportResolver();
        _genericSupportResolver = new GenericSupportResolver();
    }

    internal OfficialSupportChannelResolver(
        NvidiaSupportResolver nvidiaSupportResolver,
        IntelSupportResolver intelSupportResolver,
        AmdSupportResolver amdSupportResolver,
        GenericSupportResolver genericSupportResolver)
    {
        _nvidiaSupportResolver = nvidiaSupportResolver;
        _intelSupportResolver = intelSupportResolver;
        _amdSupportResolver = amdSupportResolver;
        _genericSupportResolver = genericSupportResolver;
    }

    public OfficialSupportChannel Resolve(DriverIdentity identity)
    {
        if (_nvidiaSupportResolver.TryResolve(identity, out var channel))
            return channel;

        if (_intelSupportResolver.TryResolve(identity, out channel))
            return channel;

        if (_amdSupportResolver.TryResolve(identity, out channel))
            return channel;

        return _genericSupportResolver.Resolve(identity);
    }
}

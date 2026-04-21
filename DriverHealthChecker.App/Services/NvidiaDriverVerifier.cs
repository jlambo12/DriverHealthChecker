namespace DriverHealthChecker.App;

internal sealed class NvidiaDriverVerifier : IVendorDriverVerifier
{
    private const string NvidiaVendorId = "10DE";
    private const string NvidiaVendorName = "NVIDIA";

    private readonly VendorDriverVerificationFlow _verificationFlow;
    private readonly VendorDefinition _vendorDefinition;

    public NvidiaDriverVerifier()
        : this(
            new VendorDriverVerificationFlow(
                new DriverIdentityTokenExtractor(),
                new DriverVersionComparer()),
            new NvidiaStubVersionSource())
    {
    }

    internal NvidiaDriverVerifier(VendorDriverVerificationFlow verificationFlow, IVendorVersionSource versionSource)
    {
        _verificationFlow = verificationFlow;
        _vendorDefinition = new VendorDefinition(
            NvidiaVendorId,
            NvidiaVendorName,
            versionSource,
            VerificationSourceType.OfficialApi);
    }

    public bool CanHandle(DriverIdentity identity)
    {
        return DriverIdentityVendorMatcher.IsNvidia(identity);
    }

    public DriverVerificationResult Verify(DriverIdentity identity)
    {
        return _verificationFlow.Verify(identity, _vendorDefinition);
    }
}

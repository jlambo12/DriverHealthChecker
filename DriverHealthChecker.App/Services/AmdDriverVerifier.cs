using System;

namespace DriverHealthChecker.App;

internal sealed class AmdDriverVerifier : IVendorDriverVerifier
{
    private const string AmdVendorId = "1002";
    private const string AmdVendorName = "AMD";

    private readonly VendorDriverVerificationFlow _verificationFlow;
    private readonly VendorDefinition _vendorDefinition;
    private readonly IDriverIdentityTokenExtractor _tokenExtractor;

    public AmdDriverVerifier()
        : this(
            new VendorDriverVerificationFlow(
                new DriverIdentityTokenExtractor(),
                new DriverVersionComparer()),
            new DriverIdentityTokenExtractor(),
            new AmdStubVersionSource())
    {
    }

    internal AmdDriverVerifier(
        VendorDriverVerificationFlow verificationFlow,
        IDriverIdentityTokenExtractor tokenExtractor,
        IVendorVersionSource versionSource)
    {
        _verificationFlow = verificationFlow ?? throw new ArgumentNullException(nameof(verificationFlow));
        _tokenExtractor = tokenExtractor ?? throw new ArgumentNullException(nameof(tokenExtractor));
        _vendorDefinition = new VendorDefinition(
            AmdVendorId,
            AmdVendorName,
            versionSource ?? throw new ArgumentNullException(nameof(versionSource)),
            VerificationSourceType.OfficialApi);
    }

    public bool CanHandle(DriverIdentity identity)
    {
        if (!_tokenExtractor.TryExtract(identity, out var tokens))
            return false;

        return string.Equals(tokens.VendorId, AmdVendorId, StringComparison.Ordinal);
    }

    public DriverVerificationResult Verify(DriverIdentity identity)
    {
        return _verificationFlow.Verify(identity, _vendorDefinition);
    }
}

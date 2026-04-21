using System;

namespace DriverHealthChecker.App;

internal sealed class IntelDriverVerifier : IVendorDriverVerifier
{
    private const string IntelVendorId = "8086";
    private const string IntelVendorName = "Intel";

    private readonly VendorDriverVerificationFlow _verificationFlow;
    private readonly VendorDefinition _vendorDefinition;
    private readonly IDriverIdentityTokenExtractor _tokenExtractor;

    public IntelDriverVerifier()
        : this(
            new VendorDriverVerificationFlow(
                new DriverIdentityTokenExtractor(),
                new DriverVersionComparer()),
            new DriverIdentityTokenExtractor(),
            new IntelStubVersionSource())
    {
    }

    internal IntelDriverVerifier(
        VendorDriverVerificationFlow verificationFlow,
        IDriverIdentityTokenExtractor tokenExtractor,
        IVendorVersionSource versionSource)
    {
        _verificationFlow = verificationFlow ?? throw new ArgumentNullException(nameof(verificationFlow));
        _tokenExtractor = tokenExtractor ?? throw new ArgumentNullException(nameof(tokenExtractor));
        _vendorDefinition = new VendorDefinition(
            IntelVendorId,
            IntelVendorName,
            versionSource ?? throw new ArgumentNullException(nameof(versionSource)),
            VerificationSourceType.OfficialApi);
    }

    public bool CanHandle(DriverIdentity identity)
    {
        if (!_tokenExtractor.TryExtract(identity, out var tokens))
            return false;

        return string.Equals(tokens.VendorId, IntelVendorId, StringComparison.Ordinal);
    }

    public DriverVerificationResult Verify(DriverIdentity identity)
    {
        return _verificationFlow.Verify(identity, _vendorDefinition);
    }
}

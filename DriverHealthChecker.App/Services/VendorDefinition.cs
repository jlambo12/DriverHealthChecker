using System;

namespace DriverHealthChecker.App;

internal sealed class VendorDefinition
{
    public VendorDefinition(
        string vendorId,
        string vendorName,
        IVendorVersionSource source,
        VerificationSourceType sourceType)
    {
        if (string.IsNullOrWhiteSpace(vendorId))
            throw new ArgumentException("VendorId must not be null or empty.", nameof(vendorId));

        if (string.IsNullOrWhiteSpace(vendorName))
            throw new ArgumentException("VendorName must not be null or empty.", nameof(vendorName));

        Source = source ?? throw new ArgumentNullException(nameof(source));
        VendorId = vendorId;
        VendorName = vendorName;
        SourceType = sourceType;
    }

    public string VendorId { get; }
    public string VendorName { get; }
    public IVendorVersionSource Source { get; }
    public VerificationSourceType SourceType { get; }
}

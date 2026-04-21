namespace DriverHealthChecker.App;

internal enum DriverVerificationStatus
{
    UpToDate,
    UpdateAvailable,
    UnableToVerifyReliably
}

internal enum VerificationSourceType
{
    OfficialApi,
    OfficialWebsite,
    LocalCache,
    Unknown
}

internal enum VerificationFailureReasonType
{
    NoMatchingVendor,
    DeviceNotInOfficialDataset,
    VerifierNotImplemented,
    VerificationFailed
}

internal enum OfficialSupportChannelType
{
    InstalledOfficialApp,
    OfficialAppInstall,
    DirectDriverPage,
    ExactSupportPage,
    ManualExplanation
}

internal enum DriverUpdateActionType
{
    OpenApp,
    OpenUrl,
    ShowExplanation
}

namespace DriverHealthChecker.App;

internal enum DriverVerificationStatus
{
    UpToDate,
    UpdateAvailable,
    UnableToVerifyReliably
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

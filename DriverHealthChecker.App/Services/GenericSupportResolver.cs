namespace DriverHealthChecker.App;

internal sealed class GenericSupportResolver
{
    public OfficialSupportChannel Resolve(DriverIdentity identity)
    {
        var displayName = string.IsNullOrWhiteSpace(identity.DisplayName)
            ? "Manual review required"
            : identity.DisplayName;

        return new OfficialSupportChannel
        {
            Type = OfficialSupportChannelType.ManualExplanation,
            Target = string.Empty,
            DisplayName = displayName,
            Description = "Точный официальный канал обновления пока не определён. Нужна ручная проверка официального источника."
        };
    }
}

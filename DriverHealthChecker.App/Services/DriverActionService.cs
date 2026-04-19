using System.Diagnostics;

namespace DriverHealthChecker.App;

internal interface IDriverActionService
{
    DriverActionFeedback Execute(DriverItem driver);
}

internal sealed class DriverActionService : IDriverActionService
{
    private readonly IOnlineActionGuard _onlineActionGuard;
    private readonly IOnlineTargetValidator _onlineTargetValidator;
    private readonly ILocalAppValidator _localAppValidator;

    public DriverActionService(
        IOnlineActionGuard onlineActionGuard,
        IOnlineTargetValidator onlineTargetValidator,
        ILocalAppValidator localAppValidator)
    {
        _onlineActionGuard = onlineActionGuard;
        _onlineTargetValidator = onlineTargetValidator;
        _localAppValidator = localAppValidator;
    }

    public DriverActionFeedback Execute(DriverItem driver)
    {
        switch (driver.OfficialAction.Kind)
        {
            case OfficialActionKind.Url:
                if (!_onlineActionGuard.CanOpenOnlineAction(out var networkMessage))
                {
                    return new DriverActionFeedback
                    {
                        Kind = DriverActionFeedbackKind.Warning,
                        Title = "Нет сети",
                        Message = networkMessage
                    };
                }

                if (!_onlineTargetValidator.IsValidUrl(driver.OfficialAction.Target))
                {
                    return new DriverActionFeedback
                    {
                        Kind = DriverActionFeedbackKind.Warning,
                        Title = "Некорректная ссылка",
                        Message = "Ссылка на официальный источник некорректна."
                    };
                }

                OpenTarget(driver.OfficialAction.Target);
                return DriverActionFeedback.None();

            case OfficialActionKind.LocalApp:
                if (!_localAppValidator.Exists(driver.OfficialAction.Target))
                {
                    return new DriverActionFeedback
                    {
                        Kind = DriverActionFeedbackKind.Warning,
                        Title = "Приложение не найдено",
                        Message = "Локальное приложение не найдено. Используй официальный сайт производителя."
                    };
                }

                OpenTarget(driver.OfficialAction.Target);
                return DriverActionFeedback.None();

            case OfficialActionKind.WindowsUpdate:
                OpenTarget("ms-settings:windowsupdate");
                return DriverActionFeedback.None();

            default:
                return new DriverActionFeedback
                {
                    Kind = DriverActionFeedbackKind.Info,
                    Title = "Официальный источник",
                    Message = driver.OfficialAction.Message
                };
        }
    }

    private static void OpenTarget(string target)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = target,
            UseShellExecute = true
        });
    }
}

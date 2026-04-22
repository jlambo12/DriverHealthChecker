using System;

namespace DriverHealthChecker.App;

internal interface IUpdateActionResolver
{
    UpdateAction Resolve(DriverUpdateContext context);
}

internal sealed class DeterministicUpdateActionResolver : IUpdateActionResolver
{
    public UpdateAction Resolve(DriverUpdateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Channel.Type switch
        {
            OfficialSupportChannelType.InstalledOfficialApp => ResolveInstalledApp(context),
            OfficialSupportChannelType.OfficialAppInstall => ResolveOfficialAppInstall(context),
            OfficialSupportChannelType.DirectDriverPage => ResolveExactPage(
                context,
                "Точный direct update path для этого драйвера ещё не реализован. Без verified exact route переход заблокирован."),
            OfficialSupportChannelType.ExactSupportPage => ResolveExactPage(
                context,
                "Точный support path для этого драйвера ещё не реализован. Без verified exact route переход заблокирован."),
            _ => BuildExplanationAction(
                context,
                "Точный официальный путь обновления пока не определён.")
        };
    }

    private static UpdateAction ResolveInstalledApp(DriverUpdateContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Channel.Target))
        {
            return BuildExplanationAction(
                context,
                "Официальное приложение отмечено как установленное, но путь запуска не задан.");
        }

        return new UpdateAction
        {
            Channel = context.Channel,
            ActionType = UpdateActionType.OpenApp,
            DisplayText = BuildInstalledAppText(context.Channel),
            Explanation = BuildOpenExplanation(context.Channel)
        };
    }

    private static UpdateAction ResolveOfficialAppInstall(DriverUpdateContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Channel.Target))
        {
            return BuildExplanationAction(
                context,
                "Официальная страница установки приложения не задана.");
        }

        return new UpdateAction
        {
            Channel = context.Channel,
            ActionType = UpdateActionType.OpenUrl,
            DisplayText = BuildOfficialAppInstallText(context.Channel),
            Explanation = BuildOpenExplanation(context.Channel)
        };
    }

    private static UpdateAction ResolveExactPage(DriverUpdateContext context, string fallbackExplanation)
    {
        if (!context.HasVerifiedExactTarget)
            return BuildExplanationAction(context, fallbackExplanation);

        if (string.IsNullOrWhiteSpace(context.Channel.Target))
        {
            return BuildExplanationAction(
                context,
                "Exact route помечен как verified, но navigation target не задан.");
        }

        return new UpdateAction
        {
            Channel = context.Channel,
            ActionType = UpdateActionType.OpenUrl,
            DisplayText = BuildExactRouteText(context.Channel),
            Explanation = BuildOpenExplanation(context.Channel)
        };
    }

    private static UpdateAction BuildExplanationAction(DriverUpdateContext context, string fallbackExplanation)
    {
        return new UpdateAction
        {
            Channel = context.Channel,
            ActionType = UpdateActionType.ShowExplanation,
            DisplayText = "Показать пояснение",
            Explanation = BuildExplanation(context.Channel, fallbackExplanation)
        };
    }

    private static string BuildInstalledAppText(OfficialSupportChannel channel)
    {
        return string.IsNullOrWhiteSpace(channel.DisplayName)
            ? "Открыть официальное приложение"
            : $"Открыть {channel.DisplayName}";
    }

    private static string BuildOfficialAppInstallText(OfficialSupportChannel channel)
    {
        return string.IsNullOrWhiteSpace(channel.DisplayName)
            ? "Открыть установку официального приложения"
            : $"Установить {channel.DisplayName}";
    }

    private static string BuildExactRouteText(OfficialSupportChannel channel)
    {
        if (!string.IsNullOrWhiteSpace(channel.DisplayName))
            return $"Открыть {channel.DisplayName}";

        return channel.Type == OfficialSupportChannelType.ExactSupportPage
            ? "Открыть точную страницу поддержки"
            : "Открыть точную страницу драйвера";
    }

    private static string BuildOpenExplanation(OfficialSupportChannel channel)
    {
        return string.IsNullOrWhiteSpace(channel.Description)
            ? "Доступен официальный путь обновления."
            : channel.Description;
    }

    private static string BuildExplanation(OfficialSupportChannel channel, string fallbackExplanation)
    {
        if (string.IsNullOrWhiteSpace(channel.Description))
            return fallbackExplanation;

        if (string.Equals(channel.Description, fallbackExplanation, StringComparison.Ordinal))
            return channel.Description;

        return $"{channel.Description} {fallbackExplanation}".Trim();
    }
}

// Copyright (c) Tai-Yng. MIT license.

using System;
using Pomodoro.Core;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Pomodoro.CmdPal;

/// <summary>
/// Completion reminders are OS-level scheduled toasts: the extension process may die at
/// any moment, the notification still fires on time. Managed per phase — scheduling a new
/// phase clears any previously scheduled toast.
/// </summary>
internal static class ToastScheduler
{
    private static ToastNotifier Notifier() => ToastNotificationManager.CreateToastNotifier(
        Windows.ApplicationModel.AppInfo.Current.AppUserModelId);

    public static void ScheduleForPhase(PomodoroPhase phase, DateTimeOffset deliverAt)
    {
        Clear();

        var xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
        var texts = xml.GetElementsByTagName("text");
        texts[0].AppendChild(xml.CreateTextNode(
            phase == PomodoroPhase.Focus ? "Focus session complete" : "Break is over"));
        texts[1].AppendChild(xml.CreateTextNode(
            phase == PomodoroPhase.Focus ? "Take a 5 minute break." : "Start the next focus session."));

        var toast = new ScheduledToastNotification(xml, deliverAt);
        Notifier().AddToSchedule(toast);
    }

    public static void Clear()
    {
        try
        {
            var notifier = Notifier();
            foreach (var scheduled in notifier.GetScheduledToastNotifications())
            {
                notifier.RemoveFromSchedule(scheduled);
            }
        }
        catch (Exception)
        {
            // toast scheduling is best-effort; the state file remains the source of truth
        }
    }
}

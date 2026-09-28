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

        // Focus end = alarm scenario (persistent + looping alarm sound until dismissed);
        // break end = reminder scenario (persistent, silent). Both carry a system-handled
        // dismiss button so no activation code is needed.
        var isFocus = phase == PomodoroPhase.Focus;
        var scenario = isFocus ? "alarm" : "reminder";
        var audio = isFocus ? "<audio loop=\"true\" src=\"ms-winsoundevent:Notification.Looping.Alarm\"/>" : string.Empty;
        var title = isFocus ? "Focus session complete" : "Break is over";
        var body = isFocus ? "Take a 5 minute break." : "Start the next focus session.";

        var xml =
            $"<toast scenario=\"{scenario}\">" +
            "<visual><binding template=\"ToastGeneric\">" +
            $"<text>{title}</text>" +
            $"<text>{body}</text>" +
            "</binding></visual>" +
            audio +
            "<actions>" +
            "<action activationType=\"system\" arguments=\"dismiss\" content=\"Dismiss\"/>" +
            "</actions>" +
            "</toast>";

        var doc = new XmlDocument();
        doc.LoadXml(xml);
        var toast = new ScheduledToastNotification(doc, deliverAt);
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

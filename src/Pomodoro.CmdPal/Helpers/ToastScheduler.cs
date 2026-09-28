// Copyright (c) Tai-Yng. MIT license.

using System;
using System.IO;
using Pomodoro.Core;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Pomodoro.CmdPal;

/// <summary>
/// Completion reminders are OS-level scheduled toasts: the extension process may die at
/// any moment, the notification still fires on time. Managed per phase — scheduling a new
/// phase clears any previously scheduled toast.
///
/// Focus end = alarm scenario (persistent on screen) with a synthesized N-chime alarm
/// (the toast audio loop attribute is all-or-nothing, so the repeat count is baked into
/// the WAV); break end = reminder scenario with the default sound. Both carry a
/// system-handled dismiss button so no activation code is needed.
/// </summary>
internal static class ToastScheduler
{
    private static ToastNotifier Notifier() => ToastNotificationManager.CreateToastNotifier(
        Windows.ApplicationModel.AppInfo.Current.AppUserModelId);

    public static void ScheduleForPhase(PomodoroPhase phase, DateTimeOffset deliverAt, int alarmChimes)
    {
        Clear();

        var isFocus = phase == PomodoroPhase.Focus;
        var scenario = isFocus ? "alarm" : "reminder";
        var audio = isFocus
            ? $"<audio src=\"ms-appdata:///LocalState/{WriteAlarmWav(alarmChimes)}\"/>"
            : "<audio silent=\"true\"/>";
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

    /// <summary>Writes the chime WAV into the app's LocalState; returns the file name.</summary>
    private static string WriteAlarmWav(int chimes)
    {
        const string fileName = "pomodoro-alarm.wav";
        try
        {
            var path = Path.Combine(
                Windows.Storage.ApplicationData.Current.LocalFolder.Path, fileName);
            File.WriteAllBytes(path, AlarmWav.Generate(chimes));
        }
        catch (Exception)
        {
            // fall back to the default toast sound if the custom file cannot be written
        }
        return fileName;
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

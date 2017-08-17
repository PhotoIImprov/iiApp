using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using UserNotifications;
using Xamarin.Forms;

[assembly: Dependency(typeof(ImageImprov.iOS.Notifications))]
namespace ImageImprov.iOS {
    public class Notifications : INotifications {
        public Notifications() {

        }

        public void SetupNotification(string title, string message, DateTime executeTime, long requestId) {
            CheckNotificationPriviledges();
            UNNotificationContent content = BuildNotificationContent(title, message);
            ScheduleNotification(content, executeTime, requestId.ToString());
        }

        public void RequestAuthorization() {
            Debug.WriteLine("DHB:Notifications:RequestAuthorization start");
            UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge, (approved, err) =>{  });
            /*
            if (Xamarin.Forms.UIDevice.CurrentDevice.CheckSystemVersion(10, 0)) {
                // Request Permissions
                UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge, (granted, error) =>
                {
                    // Do something if needed
                });
            } else if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0)) {
                var notificationSettings = UIUserNotificationSettings.GetSettingsForTypes(
                UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound, null);

                app.RegisterUserNotificationSettings(notificationSettings);
            }
            */
            Debug.WriteLine("DHB:Notifications:RequestAuthorization complete");
        }

        public bool CheckNotificationPriviledges() {
            // Get current notification settings
            bool alertsAllowed = false;
            //while (!alertsAllowed) {
            UNUserNotificationCenter.Current.GetNotificationSettings((settings) => {
                alertsAllowed = (settings.AlertSetting == UNNotificationSetting.Enabled);
            });

            Debug.WriteLine("DHB:Notifications:CheckNotificationPriveledges allowed?" + alertsAllowed.ToString());

            if (!alertsAllowed) { RequestAuthorization(); }
            //}
            return alertsAllowed;
        }

        public UNMutableNotificationContent BuildNotificationContent(string title, string message) {
            UNMutableNotificationContent content = new UNMutableNotificationContent();
            content.Title = title;
            //content.Subtitle = "";  keep blank for now.
            content.Body = message;
            //content.Badge = 1;  hold off on this. I want only 1 at a time...
            return content;
        }

        public void ScheduleNotification(UNNotificationContent content, DateTime notifyTime, string requestId) {
            Debug.WriteLine("DHB:Notifications:ScheduleNotification begin");
            Foundation.NSDateComponents notificationContentNSCDate = new Foundation.NSDateComponents();
            notificationContentNSCDate.Year = notifyTime.Year;
            notificationContentNSCDate.Month = notifyTime.Month;
            notificationContentNSCDate.Day = notifyTime.Day;
            notificationContentNSCDate.Hour = notifyTime.Hour;
            notificationContentNSCDate.Minute = notifyTime.Minute;
            notificationContentNSCDate.Second = notifyTime.Second;
            //notificationContentNSCDate.TimeZone = 
            Debug.WriteLine("DHB:Notifications:ScheduleNotification sched time:" + notificationContentNSCDate.ToString());
            Debug.WriteLine("DHB:Notifications:ScheduleNotification now  time:" + notifyTime.ToString());
            // repeats makes no sense as just a bool. what does that mean? everyday, every 5 mins, wat wat?
            UNNotificationTrigger trigger = UNCalendarNotificationTrigger.CreateTrigger(notificationContentNSCDate, true);

            //string requestId = "firstRequest";
            var request = UNNotificationRequest.FromIdentifier(requestId, content, trigger);

            UNUserNotificationCenter.Current.AddNotificationRequest(request, (err) =>
            {
                if (err != null) {
                    Debug.WriteLine("DHB:iOS:Notifications:ScheduleNotification err:" + err.ToString());
                }
            });
            Debug.WriteLine("DHB:Notifications:ScheduleNotification scheduled");
        }

        public void RetrieveNotifications() {
            UNNotificationRequest[] pendingNotifications = new UNNotificationRequest[] { };
            //UNUserNotificationCenter.Current.GetPendingNotificationRequests(pendingNotifications);
            UNUserNotificationCenter.Current.GetPendingNotificationRequests((result) => {
                Debug.WriteLine("DHB:iOS:Notifications:RetrieveNotifications " + result.ToString());
                Debug.WriteLine("DHB:iOS:Notifications:RetrieveNotifications " + result.Length);
                foreach(UNNotificationRequest req in result) {
                    Debug.WriteLine("DHB:iOS:Notifications:RetrieveNotifications " + req.ToString());
                }
                Debug.WriteLine("DHB:iOS:Notifications:RetrieveNotifications end result");
            });
        }
    }
}

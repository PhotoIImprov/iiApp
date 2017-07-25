using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Xamarin.Forms;

[assembly: Dependency(typeof(ImageImprov.Droid.Notifications))]
namespace ImageImprov.Droid {
    //[IntentFilter(new[] { Intent.ActionBootCompleted })]  //this is for on boot- up not what I'm currently looking for....
    [BroadcastReceiver (Exported = true, Enabled = true)]
    public class Notifications : BroadcastReceiver, INotifications {
        Context context;

        /// <summary>
        /// Tracks request codes so that I dont step on my own notifications.
        /// </summary>
        int requestCode;

        public Notifications() { }  // needed for auto-generated code.

        public Notifications(Context context) {
            this.context = context;

            // @todo set this by looking up my pending notifications
            requestCode = 0;

            /* This does pop it onto the queue. Kind of useless though as it goes away when you close the app.
            Notification.Builder builder = new Notification.Builder(context)
                .SetContentTitle("Image Improv")
                .SetContentText("Test 1!")
                .SetSmallIcon(Resource.Drawable.icon);

            Notification note = builder.Build();

            NotificationManager notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;

            const int notificationId = 0;
            notificationManager.Notify(notificationId, note);
            */
        }

        public void SetupNotification(string title, string message, DateTime executeTime, long requestId) {
            Remind(executeTime, title, message, requestId);
        }

        public void RequestAuthorization() {
        }

        public bool CheckNotificationPriveledges() {
            return true;
        }

        public void BuildNotificationContent() {
        }

        public void ScheduleNotification() {
            System.Diagnostics.Debug.WriteLine("DHB:Notifications:ScheduleNotification scheduled");
        }

        public void RetrieveNotifications() {
            System.Diagnostics.Debug.WriteLine("DHB:iOS:Notifications:RetrieveNotifications end result");
        }

        /// <summary>
        /// Creates the alarm to wake the app, to set the notification.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="title"></param>
        /// <param name="message"></param>
        public void Remind (DateTime notificationTime, string title, string message, long requestId) {
            Intent alarmIntent = new Intent(Forms.Context, typeof(ImageImprov.Droid.Notifications));  // this class is the intent as well! :)
            alarmIntent.PutExtra("message", message);
            alarmIntent.PutExtra("title", title);

            PendingIntent pendingIntent = PendingIntent.GetBroadcast(Forms.Context, (int)requestId, alarmIntent, PendingIntentFlags.OneShot);
            requestCode++;
            AlarmManager alarmManager = (AlarmManager)Forms.Context.GetSystemService(Context.AlarmService);

            long launchTime = GlobalSingletonHelpers.GetMillisecondsSinceUnixEpoch(notificationTime);
            alarmManager.Set(AlarmType.RtcWakeup, launchTime, pendingIntent);
            

            /*
            // sanity check.  Turns out I'm sane.
            long now = Java.Lang.JavaSystem.CurrentTimeMillis();
            System.Diagnostics.Debug.WriteLine("DHB:Droid:Notifications:Remind launch time is:" + launchTime);
            System.Diagnostics.Debug.WriteLine("DHB:Droid:Notifications:Remind droid  time is:" + now);
            */
            // don't understand the purpose of this line...
            //PendingIntent pendingIntent2 = PendingIntent.GetBroadcast(context, requestCode, alarmIntent, 0);  this crashes the system, but then requestCode is wrong...
            //PendingIntent pendingIntent2 = PendingIntent.GetBroadcast(context, (requestCode-1), alarmIntent, 0);
        }


        public override void OnReceive(Context context, Intent intent) {
            System.Diagnostics.Debug.WriteLine("DHB:Droid:Notifications:OnReceive firing");
            var message = intent.GetStringExtra("message");
            var title = intent.GetStringExtra("title");

            Intent backIntent = new Intent(context, typeof(MainActivity));
            backIntent.SetFlags(ActivityFlags.NewTask);


            PendingIntent contentIntent = PendingIntent.GetActivity(context, 0, backIntent, PendingIntentFlags.OneShot);

            //Generate a notification with just short text and small icon
            var builder = new Notification.Builder(context)
                            .SetContentIntent(contentIntent)
                            .SetSmallIcon(Resource.Drawable.icon)
                            .SetContentTitle(title)
                            .SetContentText(message)
                            //.SetStyle(style)
                            .SetWhen(Java.Lang.JavaSystem.CurrentTimeMillis())
                            .SetAutoCancel(true);

            var notification = builder.Build();

            NotificationManager manager = context.GetSystemService(Context.NotificationService) as NotificationManager;
            manager.Notify(1331, notification);
        }

    }
}
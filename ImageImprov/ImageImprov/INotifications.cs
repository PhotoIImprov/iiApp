using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageImprov {
    /// <summary>
    /// Defines the interface for the dependency class for notifications.
    /// </summary>
    public interface INotifications {
        /// <summary>
        /// Puts a notification onto the queue.
        /// </summary>
        /// <param name="title">The name of the notification, generally Image Improv</param>
        /// <param name="message">What we are saying. The initial messages are 1 of 2 category messages, saying what the category is, and how long till it ends.</param>
        /// <param name="executeTime">When to post the message</param>
        /// <param name="requestId">An id for the message. Will pass in the category id, so that I can establish existing notifications on app restart.</param>
        void SetupNotification(string title, string message, DateTime executeTime, long requestId);
    }
}

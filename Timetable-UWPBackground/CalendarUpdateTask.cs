using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timetable_UWPCacheManager;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Notifications;

namespace Timetable_UWPBackground
{
    public sealed class CalendarUpdateTask : IBackgroundTask
    {

        BackgroundTaskDeferral _deferral;

        private ApplicationDataContainer _Settings = ApplicationData.Current.LocalSettings;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            await BackgroundUpdate();

            _deferral.Complete();
        }

        private async Task BackgroundUpdate()
        {
            string token;
            string id;
            int reminderIndex = 0;

            // Check login status
            if (!_Settings.Values.ContainsKey("token") || string.IsNullOrEmpty(token = _Settings.Values["token"].ToString()))
            {
                return;
            }
            if (!_Settings.Values.ContainsKey("id") || string.IsNullOrEmpty(id = _Settings.Values["id"].ToString()))
            {
                return;
            }
            if (!_Settings.Values.ContainsKey("remind-index"))
            {
                object o = _Settings.Values["remind-index"];
                if (o is int)
                {
                    reminderIndex = (int) o; 
                }
                return;
            }

            var result = await ClassCacheManagerUWP.Instance.UpdateTimetable(id, token, reminderIndex);
            if (result.success)
            {
                ToastContent toastContent = new ToastContent()
                {
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                        {
                            new  AdaptiveText { Text = "XJTLU Timetable Auto-update" },
                            new  AdaptiveText { Text = "Update successfully" }
                        }
                        }
                    }
                };

                // Send the success toast notification
                var toastNotif = new ToastNotification(toastContent.GetXml());
                ToastNotificationManager.CreateToastNotifier().Show(toastNotif);
            }
            else
            {
                ToastContent toastContent = new ToastContent()
                {
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                        {
                            new  AdaptiveText { Text = "XJTLU Timetable Auto-update" },
                            new  AdaptiveText { Text = "Update failed, please re-login your account" }
                        }
                        }
                    }
                };

                // Send the success toast notification
                var toastNotif = new ToastNotification(toastContent.GetXml());
                ToastNotificationManager.CreateToastNotifier().Show(toastNotif);
            }

        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Exchange.WebServices.Data;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using System.Globalization;
using Windows.Security.Credentials;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Timetable_Core;
using Timetable_UWPCacheManager;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace XJTLU_Timetable_UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPageViewModel ViewModel;

        public const string BGUpdateTask_Name = "TimetableUpdateTask";
        public const string BGUpdateTask_EntryPoint = "Timetable_UWPBackground.CalendarUpdateTask";
        private BackgroundTaskRegistration BGUpdateTask;

        public const string BGServiceCompleteTask_Name = "ServiceCompleteTask";
        public const string BGServiceCOmpleteTask_EntryPoint = "Timetable_UWPBackground.ServiceComplete";
        private BackgroundTaskRegistration BGServiceCompleteTask;

        private string AccountId;

        private ApplicationDataContainer _Settings;

        public MainPage()
        {
            this.InitializeComponent();
            ViewModel = new MainPageViewModel();
            ViewModel.CurrentDateDisplay = "Current date: " + DateTime.Now.ToShortDateString();

            _Settings = ApplicationData.Current.LocalSettings;
            if (!_Settings.Values.ContainsKey("remind-index"))
            {
                _Settings.Values["remind-index"] = 4;
                _RemindSelectionBox.SelectedIndex = 4; // 15 mins
            }
            else
            {
                object obj = _Settings.Values["remind-index"];
                if (!(obj is int) || (int) obj > 7)
                {
                    _Settings.Values["remind-index"] = 4;
                    _RemindSelectionBox.SelectedIndex = 4;
                }
                else
                {
                    _RemindSelectionBox.SelectedIndex = (int) obj;
                }
            }

            ViewModel.AutoUpdate = true;
            if (_Settings.Values.ContainsKey("auto-update"))
            {
                object obj = _Settings.Values["auto-update"];
                if (obj is bool && (!(bool) obj) )
                {
                    ViewModel.AutoUpdate = false;
                }
            }

            // Check login status
            if (!_Settings.Values.ContainsKey("token") || string.IsNullOrEmpty(_Settings.Values["token"].ToString()))
            {
                VisualStateManager.GoToState(this, LoginState.Name, true);
                return;
            }
            if (!_Settings.Values.ContainsKey("id") || string.IsNullOrEmpty(_Settings.Values["id"].ToString()))
            {
                VisualStateManager.GoToState(this, LoginState.Name, true);
                return;
            }
            if (!_Settings.Values.ContainsKey("username") || string.IsNullOrEmpty(_Settings.Values["username"].ToString()))
            {
                VisualStateManager.GoToState(this, LoginState.Name, true);
                return;
            }

            AccountId = _Settings.Values["id"].ToString();
            ViewModel.Username = _Settings.Values["username"].ToString();
            PasswordVault vault = new PasswordVault();
            PasswordCredential credential = vault.Retrieve("XJTLUAccount", ViewModel.Username);
            credential.RetrievePassword();
            ClassCacheManagerUWP.Instance.Service.Credentials = new WebCredentials(credential.UserName, credential.Password, "xjtlu.edu.cn");
            VisualStateManager.GoToState(this, MainState.Name, true);

            // post stage
            LoadPreview();
            RegisterBackgroundTask();
        }

        private async void LoadPreview()
        {
            ClassCache cache = await ClassCacheManagerUWP.Instance.LoadCache(WeekHelper.GetStartDayOfWeek(), AccountId);
            if (cache != null)
            {
                UpdateAndDisplayPreview(cache.ClassList);
            }
        }

        private void UpdateAndDisplayPreview(List<Class> classList)
        {
            classList.ForEach(cls => { ViewModel.ClassPreviewList.Add(cls); });
            VisualStateManager.GoToState(this, MainPreviewState.Name, true);
        }

        private async void Button_Login_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsUpdating = true;
            if (string.IsNullOrEmpty(ViewModel.Username))
            {
                return;
            }
            if (string.IsNullOrEmpty(_PasswordBox.Password))
            {
                return;
            }

            XJTLUAccount account = new XJTLUAccount
            {
                Username = ViewModel.Username,
                Password = _PasswordBox.Password
            };
            if (account.IsTestAccount())
            {
                VisualStateManager.GoToState(this, MainState.Name, true);
                ViewModel.IsUpdating = false;
                AccountId = "0000-0000-0000-0000";
                LoadPreview();
                return;
            }

            try
            {
                string responseStr = "";

                using (HttpClient client = new HttpClient())
                {
                    // Login to portal app
                    string acc = JsonConvert.SerializeObject(account);
                    StringContent content = new StringContent(acc, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("http://portalapp.xjtlu.edu.cn/edu-app/api/authentication/login", content);
                    responseStr = await response.Content.ReadAsStringAsync();
                }

                // Get token and student internal id
                JObject rootObj = JObject.Parse(responseStr);
                if (rootObj["success"].Type != JTokenType.Boolean || !rootObj["success"].ToObject<bool>())
                {
                    string ResultStr = "Login failed! \r\n";
                    string error = rootObj["data"].ToString();
                    if (!string.IsNullOrEmpty(error))
                    {
                        ResultStr += error;
                    }

                    ViewModel.LoginResultDisplay = ResultStr;
                    FlyoutBase.ShowAttachedFlyout((FrameworkElement) sender);
                    ViewModel.IsUpdating = false;
                    return;
                }

                // Store username, password, token and id
                PasswordVault vault = new PasswordVault();
                vault.Add(new PasswordCredential("XJTLUAccount", ViewModel.Username, _PasswordBox.Password));
                string token = rootObj["data"]["token"].ToString();
                string id = rootObj["data"]["userId"].ToString();
                AccountId = id;
                _Settings.Values["token"] = token;
                _Settings.Values["id"] = id;
                _Settings.Values["username"] = ViewModel.Username;

                // Login success
                VisualStateManager.GoToState(this, MainState.Name, true);
                LoadPreview();
                ViewModel.IsUpdating = false;
                return;
            }
            catch
            {
                ViewModel.LoginResultDisplay = "Login failed!";
                FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
                ViewModel.IsUpdating = false;
                return;
            }
        }

        private async void Button_UpdateTimetable_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsUpdating = true;
            if (string.Equals(ViewModel.Username, "test.testXJTLU"))
            {
                goto success;
            }

            string token = ApplicationData.Current.LocalSettings.Values["token"].ToString();
            var result = await ClassCacheManagerUWP.Instance.UpdateTimetable(AccountId, token, _RemindSelectionBox.SelectedIndex);
            if (!result.success)
            {
                await TokenExpiredDialog.ShowAsync();
                return;
            }

            List<Class> table1 = result.classCaches.First()?.ClassList;
            if (table1 != null)
            {
                UpdateAndDisplayPreview(table1);
            }

        success:
            ToastContent toastContent = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new  AdaptiveText { Text = "XJTLU Timetable" },
                            new  AdaptiveText { Text = "Your calendar has been updated" }
                        }
                    }
                }
            };

            // Send the success toast notification
            var toastNotif = new ToastNotification(toastContent.GetXml());
            ToastNotificationManager.CreateToastNotifier().Show(toastNotif);

            ViewModel.IsUpdating = false;
        }

        private void Button_Logout_Click(object sender, RoutedEventArgs e)
        {
            Logout();
        }

        private void Logout()
        {
            ApplicationData.Current.LocalSettings.Values["token"] = "";
            ApplicationData.Current.LocalSettings.Values["id"] = "";
            ApplicationData.Current.LocalSettings.Values["username"] = "";

            VisualStateManager.GoToState(this, LoginState.Name, true);
        }

        private void TokenExpiredDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Logout();
            ViewModel.IsUpdating = false;
        }

        private void _RemindSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _Settings.Values["remind-index"] = _RemindSelectionBox.SelectedIndex;
        }

        private void CheckBox_AutoUpdate_Checked(object sender, RoutedEventArgs e)
        {
            RegisterBackgroundTask();
        }

        private void CheckBox_AutoUpdate_Unchecked(object sender, RoutedEventArgs e)
        {
            UnregisterBackgroundTask();
        }

        private async void RegisterBackgroundTask()
        {
            bool regUpdate = false;
            bool regService = false;
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == BGUpdateTask_Name)
                {
                    regUpdate = true;
                }
                if (task.Value.Name == BGServiceCompleteTask_Name)
                {
                    regService = true;
                }
            }

            await BackgroundExecutionManager.RequestAccessAsync();

            if (!regUpdate)
            {
                BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
                builder.Name = BGUpdateTask_Name;
                builder.TaskEntryPoint = BGUpdateTask_EntryPoint;
                builder.SetTrigger(new TimeTrigger(1440, false)); // 60 * 24, every day
                BGUpdateTask = builder.Register();
            }

            if (!regService)
            {
                BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
                builder.Name = BGServiceCompleteTask_Name;
                builder.TaskEntryPoint = BGServiceCOmpleteTask_EntryPoint;
                builder.SetTrigger(new SystemTrigger(SystemTriggerType.ServicingComplete, false));
                BGServiceCompleteTask = builder.Register();
            }
        }

        private void UnregisterBackgroundTask()
        {
            if (BGUpdateTask != null)
            {
                BGUpdateTask.Unregister(false);
            }
        }
    }
}

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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace XJTLU_Timetable_UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPageViewModel ViewModel;

        private string AccountId;

        private ExchangeService _Service;
        private ApplicationDataContainer _Settings;

        public MainPage()
        {
            this.InitializeComponent();
            ViewModel = new MainPageViewModel();
            ViewModel.CurrentDateDisplay = "Current date: " + DateTime.Now.ToShortDateString();

            _Service = new ExchangeService();
            _Service.Url = new Uri("https://mail.xjtlu.edu.cn/EWS/Exchange.asmx");

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
            _Service.Credentials = new WebCredentials(credential.UserName, credential.Password, "xjtlu.edu.cn");
            VisualStateManager.GoToState(this, MainState.Name, true);
            LoadPreview();
        }

        private async void LoadPreview()
        {
            ClassCache cache = await ClassCache.LoadCache(WeekHelper.GetStartDayOfWeek(), AccountId);
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

            DateTime startOfWeek = WeekHelper.GetStartDayOfWeek();
            DateTime endOfWeek = startOfWeek.AddDays(WeekHelper.Interval_EndOfWeek);
            DateTime startOfNextWeek = startOfWeek.AddDays(WeekHelper.Interval_StartOfNextWeek);
            DateTime endOfNextWeek = startOfWeek.AddDays(WeekHelper.Interval_EndOfNextWeek);

            List<Class> table1 = await GetTimetable(startOfWeek, endOfWeek);
            List<Class> table2 = await GetTimetable(startOfNextWeek, endOfNextWeek);
            if (table1 == null && table2 == null)
            {
                await TokenExpiredDialog.ShowAsync();
                return;
            }

            ClassCache cache= await ClassCache.LoadCache(startOfWeek, AccountId);
            await UpdateTimetable(table1, cache, startOfWeek);

            cache = await ClassCache.LoadCache(startOfNextWeek, AccountId);
            await UpdateTimetable(table2, cache, startOfNextWeek);

            UpdateAndDisplayPreview(table1);

success:
            ToastContent toastContent = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new  AdaptiveText { Text = "XJTLU Timetable Helper" },
                            new  AdaptiveText { Text = "Your timetable have been exported to calendar" }
                        }
                    }
                }
            };

            // Send the success toast notification
            var toastNotif = new ToastNotification(toastContent.GetXml());
            ToastNotificationManager.CreateToastNotifier().Show(toastNotif);

            ViewModel.IsUpdating = false;
        }

        private async System.Threading.Tasks.Task UpdateTimetable(List<Class> table, ClassCache cache, DateTime startOfWeek)
        {
            if (cache != null)
            {
                if (cache.IsLatestCache(table, _RemindSelectionBox.SelectedIndex))
                {
                    return;
                }
                else
                {
                    // Delete appointment from calendar
                    CalendarFolder calendarFolder = await CalendarFolder.Bind(_Service, WellKnownFolderName.Calendar);
                    Microsoft.Exchange.WebServices.Data.CalendarView calendarView =
                        new Microsoft.Exchange.WebServices.Data.CalendarView(cache.StartOfWeek, cache.GetEndDayOfWeek());
                    FindItemsResults<Appointment> items = await calendarFolder.FindAppointments(calendarView);
                    foreach (Class cls in cache.ClassList)
                    {
                        foreach (Appointment apt in items)
                        {
                            if (Equals(cls.AppointmentId, apt.Id.UniqueId))
                            {
                                await apt.Delete(DeleteMode.MoveToDeletedItems);
                                break;
                            }
                        }
                    }

                    cache.ClassList = table;
                }
            }
            else
            {
                cache = new ClassCache(startOfWeek, table, AccountId);
            }

            // Add appointment to calendar
            await ExportToExchangeCalendar(table);
            await cache.SaveCache();
        }

        private async Task<List<Class>> GetTimetable(DateTime from, DateTime to)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string token = ApplicationData.Current.LocalSettings.Values["token"].ToString();
                    string id = ApplicationData.Current.LocalSettings.Values["id"].ToString();

                    // Get timetable
                    string _url = "http://portalapp.xjtlu.edu.cn/edu-app/api/userTimeTable/findStudentsTimeTablesById?userId={0}&userType=S&fromDate={1}&toDate={2}";
                    client.DefaultRequestHeaders.Add("Authorization", token);
                    HttpResponseMessage response = await client.GetAsync(string.Format(_url, id, WeekHelper.GetDateStr(from), WeekHelper.GetDateStr(to)));
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        return null;
                    }
                    string responseStr = await response.Content.ReadAsStringAsync();

                    JObject rootObj = JObject.Parse(responseStr);
                    return rootObj["data"]["timeTables"].ToObject<List<Class>>();
                }
            }
            catch
            {
                return null;
            }
        }

        private async System.Threading.Tasks.Task ExportToExchangeCalendar(List<Class> classList)
        {
            // TODO: Validate exsisting appointment
            foreach (Class cls in classList)
            {
                Appointment appointment = new Appointment(_Service);
                appointment.IsAllDayEvent = false;

                appointment.Subject = cls.ModuleCode;
                appointment.Start = cls.StartTime;
                appointment.End = cls.EndTime;
                appointment.Location = cls.Location;
                appointment.Sensitivity = Sensitivity.Private;

                StringBuilder builder = new StringBuilder();
                builder.AppendLine(cls.ModuleTypeGroup);
                builder.AppendLine(cls.StaffName);
                builder.AppendLine(cls.ModuleType);
                appointment.Body = new MessageBody(BodyType.Text, builder.ToString());

                switch (_RemindSelectionBox.SelectedIndex)
                {
                    case 0:
                        appointment.IsReminderSet = false;
                        break;
                    case 1:
                        appointment.ReminderMinutesBeforeStart = 0;
                        break;
                    case 2:
                        appointment.ReminderMinutesBeforeStart = 5;
                        break;
                    case 3:
                        appointment.ReminderMinutesBeforeStart = 10;
                        break;
                    case 4:
                        appointment.ReminderMinutesBeforeStart = 15;
                        break;
                    case 5:
                        appointment.ReminderMinutesBeforeStart = 30;
                        break;
                    case 6:
                        appointment.ReminderMinutesBeforeStart = 60;
                        break;
                    case 7:
                        appointment.ReminderMinutesBeforeStart = 120;
                        break;
                }

                // TODO: Retry after saving failed (networking issue)
                await appointment.Save();
                cls.AppointmentId = appointment.Id.UniqueId;
            }
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
    }
}

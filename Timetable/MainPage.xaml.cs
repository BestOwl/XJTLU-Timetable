using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timetable.Core;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Timetable
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        private static MainPage _Instance;
        public static MainPage Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new MainPage();
                }
                return _Instance;
            }
            set => _Instance = value;
        }

        public bool _Loading;

        public MainPage()
        {
            InitializeComponent();
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();
            if (_Loading)
            {
                return;
            }
            if (ViewModel.ClassList.Count == 0 && App.Instance.Account.IsLogined)
            {
                await LoadPreview();
            }
        }

        public async Task LoadPreview()
        {
            // Load preview from local cache
            List<Class> classList;
            if (App.Instance.Account.IsTestAccount())
            {
                classList = ClassCacheManager.LoadTestCache().ClassList;
            }
            else
            {
                classList = await ClassCacheManager.Instance.LoadClassListFromCache(WeekHelper.GetStartDayOfWeek(),
                App.Instance.Account.AccountId);
            }
                

            if (classList.Count > 0)
            {
                DisplayClassList(classList);
            }
            else
            {
                await Update();
            }
        }

        private async Task Update()
        {
            XJTLUAccount acc = App.Instance.Account;
            if (acc.IsTestAccount())
            {
                return;
            }
            var result = await ClassCacheManager.Instance.UpdateTimetable(acc.AccountId, acc.Token, Settings.ReminderIndex);
            if (result.TokenExpired)
            {
                Logout();
            }
            else
            {
                List<Class> classList = new List<Class>();
                foreach (ClassCache ca in result.classCaches)
                {
                    classList.AddRange(ca.ClassList);
                }
                DisplayClassList(classList);

            }
        }

        private void DisplayClassList(List<Class> classList)
        {
            ViewModel.ClassList.Clear();
            foreach (Class cls in classList)
            {
                if (cls.StartTime < DateTime.Now)
                {
                    continue;
                }
                ViewModel.ClassList.Add(cls);
            }
        }

        public async void Logout()
        {
            ViewModel.ClassList.Clear();
            App.Instance.Account.Logout();
            Settings.AccountId = string.Empty;
            Settings.Username = string.Empty;
            Settings.ClearToken();
            Settings.ClearPassword();
            await RootPage.Instance.Navigation.PushAsync(LoginPage.Instance);
        }

        private async void ListView_Refreshing(object sender, EventArgs e)
        {
#if DEBUG
            System.Console.WriteLine("DEBUG - Refreshing");
#endif
            ViewModel.IsRefreshing = true;
            if (!App.Instance.Account.IsTestAccount())
            {
                await Update();
            }
            ViewModel.IsRefreshing = false;
#if DEBUG
            System.Console.WriteLine("Finished refreshing");
#endif
        }
    }
}
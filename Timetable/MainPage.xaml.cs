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
        public static MainPage Instance;

        public MainPage()
        {
            InitializeComponent();
            Instance = this;

            LoadPreview();
        }

        private async void LoadPreview()
        {
            // Load preview from local cache
            ClassCache cache = await ClassCacheManager.Instance.LoadCache(WeekHelper.GetStartDayOfWeek(), 
                AppShell.Instance.Account.AccountId);
            if (cache != null)
            {
                DisplayClassList(cache.ClassList);
            }

            await Update();
        }

        private async Task Update()
        {
            XJTLUAccount acc = AppShell.Instance.Account;
            var result = await ClassCacheManager.Instance.UpdateTimetable(acc.AccountId, acc.Token, Settings.ReminderIndex, 4);
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
            Settings.ClearToken();
            await AppShell.Instance.Navigation.PushModalAsync(LoginPage.Instance);
        }

        private async void ListView_Refreshing(object sender, EventArgs e)
        {
            await Update();
        }
    }
}
using System;
using System.Threading.Tasks;
using Timetable.Core;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Timetable
{
    public partial class App : Application
    {
        public static App Instance;
        public XJTLUAccount Account = new XJTLUAccount();

        public App()
        {
            InitializeComponent();
            Instance = this;
        }

        protected override async void OnStart()
        {
            // Handle when your app starts
            Account.Token = await Settings.GetTokenAsync();
            Account.Password = await Settings.GetPasswordAsync();
            Account.Username = Settings.Username;
            Account.AccountId = Settings.AccountId;

            MainPage = AppShell.Instance;
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}

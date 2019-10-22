using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timetable.Core;
using Timetable.ViewModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Timetable
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class AppShell : Shell
    {
        public static AppShell Instance;

        public AppShellViewModel ViewModel = new AppShellViewModel();
        public XJTLUAccount Account = new XJTLUAccount();

        public AppShell()
        {
            InitializeComponent();
            Instance = this;

            Init();
        }

        public async void Init()
        {
            Account.Token = await Settings.GetTokenAsync();
            if (string.IsNullOrEmpty(Account.Token))
            {
                await Navigation.PushModalAsync(LoginPage.Instance);
                return;
            }

            Account.Username = Settings.Username;
            if (string.IsNullOrEmpty(Account.Username))
            {
                await Navigation.PushModalAsync(LoginPage.Instance);
                return;
            }

            Account.AccountId = Settings.AccountId;
            if (string.IsNullOrEmpty(Account.AccountId))
            {
                await Navigation.PushModalAsync(LoginPage.Instance);
                return;
            }
        }

        private void MenuItem_Logout_Clicked(object sender, EventArgs e)
        {
            MainPage.Instance.Logout();
        }
    }
}

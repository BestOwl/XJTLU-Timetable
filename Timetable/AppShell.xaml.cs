using Microsoft.Exchange.WebServices.Data;
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
        public static AppShell _Instance;
        public static AppShell Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new AppShell();
                }
                return _Instance;
            }
            set => _Instance = value;
        }

        public AppShell()
        {
            InitializeComponent();
            Instance = this;

            XJTLUAccount acc = App.Instance.Account;

            ViewModel.Username = acc.Username;
            ClassCacheManager.Instance.SetupExchangeAccount(acc);
        }

        private void MenuItem_Logout_Clicked(object sender, EventArgs e)
        {
            MainPage.Instance.Logout();
        }

        protected override bool OnBackButtonPressed()
        {
            return true; //Ignore back button 
        }
    }
}

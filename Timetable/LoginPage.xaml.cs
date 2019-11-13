using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timetable.Core;
using Timetable.ViewModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Timetable
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        private static LoginPage _Instance;
        public static LoginPage Instance
        {
            get
            {
                if (_Instance == null)
                {
                    return _Instance = new LoginPage();
                }
                else
                {
                    return _Instance;
                }
            }
            set { _Instance = value; }
        }

        public AppShellViewModel ViewModel = AppShell.Instance.ViewModel;

        public LoginPage()
        {
            InitializeComponent();
            Instance = this;
            BindingContext = ViewModel;
        }

        public async void Button_Login_Clicked(object sender, EventArgs args)
        {
            XJTLUAccount acc = App.Instance.Account;

            acc.Username = ViewModel.Username;
            acc.Password = ViewModel.Password;

            LoginResult result = await acc.Login();
            if (result.Result == LoginResult.ResultType.Success)
            {
                Settings.SaveAccount(acc);
                ClassCacheManager.Instance.SetupExchangeAccount(acc);
                MainPage.Instance._Loading = true;
                Task loadTask = MainPage.Instance.LoadPreview();

                await RootPage.Instance.Navigation.PopAsync();
                await loadTask;
                MainPage.Instance._Loading = false;

            }
            else
            {
                await DisplayAlert("Login Failed", result.ResultDescription, "Ok");
            }
        }

        protected override bool OnBackButtonPressed()
        {
            return true; //Ignore back button 
        }
    }
}
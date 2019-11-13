using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Timetable
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RootPage : NavigationPage
    {
        public static RootPage Instance;

        public RootPage()
        {
            InitializeComponent();
            Instance = this;
            SetHasBackButton(this, false);

            Navigation.PushAsync(AppShell.Instance);
            if (!App.Instance.Account.IsLogined)
            {
                Navigation.PushAsync(LoginPage.Instance);
            }
        }
    }
}
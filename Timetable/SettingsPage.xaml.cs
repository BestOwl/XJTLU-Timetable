using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timetable.Core;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Timetable
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();

            ViewModel.BackgroundUpdate = Settings.BackgroundUpdate;
            ViewModel.UpdateToExchange = Settings.UpdateToExchange;

            int index = Settings.ReminderIndex;
            if (!Enum.IsDefined(typeof(Reminders), index))
            {
                index = (int) Reminders.Before15Minutes;
                Settings.ReminderIndex = index;
            }
            ViewModel.ReminderIndex = (Reminders) index;
        }
    }
}
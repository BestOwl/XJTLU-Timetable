using System;
using System.Collections.Generic;
using System.Text;
using Timetable.Core;

namespace Timetable.ViewModel
{
    public class SettingsPageViewModel : BindableBase
    {
        private bool _BackgroundUpdate;
        public bool BackgroundUpdate
        {
            get => _BackgroundUpdate;
            set
            {
                SetProperty(ref _BackgroundUpdate, value);
                Settings.UpdateToExchange = value;
            }
        }

        private bool _UpdateToExchange;
        public bool UpdateToExchange
        {
            get => _UpdateToExchange;
            set 
            {
                SetProperty(ref _UpdateToExchange, value);
                Settings.UpdateToExchange = value;
            } 
        }

        private int _ReminderIndex;
        public int ReminderIndex
        {
            get => _ReminderIndex;
            set 
            {
                SetProperty(ref _ReminderIndex, value);
                Settings.ReminderIndex = value;
            } 
        }
    }
}

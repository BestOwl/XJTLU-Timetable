using System;
using System.Collections.Generic;
using System.Text;

namespace Timetable.ViewModel
{
    public class AppShellViewModel : BindableBase
    {
        private string _Username;
        public string Username
        {
            get => _Username;
            set => SetProperty(ref _Username, value);
        }

        private string _Password;
        public string Password
        {
            get => _Password;
            set => SetProperty(ref _Password, value);
        }
    }
}

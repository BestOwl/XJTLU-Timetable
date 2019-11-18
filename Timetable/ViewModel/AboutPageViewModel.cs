using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Essentials;

namespace Timetable.ViewModel
{
    public class AboutPageViewModel : BindableBase
    {
        public string VersionString
        {
            get => AppInfo.VersionString;
        }
    }
}

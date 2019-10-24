using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

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

        private ImageSource _PhotoImageSource;
        public ImageSource PhotoImageSource
        {
            get
            {
                if (_PhotoImageSource == null)
                {
                    FontImageSource ret = new FontImageSource
                    {
                        Glyph = "\uE11D",
                        Color = Color.Black,
                        FontFamily = (OnPlatform<string>)Application.Current.Resources["MDL2Symbols"]
                    };
                    return ret;
                }
                return _PhotoImageSource;
            }
            set => SetProperty(ref _PhotoImageSource, value);
        }
    }
}

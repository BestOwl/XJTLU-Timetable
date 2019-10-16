using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Timetable.Core;

namespace Timetable.ViewModel
{
    public class MainPageViewModel : BindableBase
    {
        private ObservableCollection<Class> _ClassList;
        public ObservableCollection<Class> ClassList
        {
            get => _ClassList;
            set => SetProperty(ref _ClassList, value);
        }

        private bool _BackgroundUpdate;
        public bool BackgroundUpdate
        {
            get => _BackgroundUpdate;
            set => SetProperty(ref _BackgroundUpdate, value);
        }
        
    }
}

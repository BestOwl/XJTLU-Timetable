using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Timetable.Core;

namespace Timetable.ViewModel
{
    public class MainPageViewModel : BindableBase
    {
        private ObservableCollection<Class> _ClassList = new ObservableCollection<Class>();
        public ObservableCollection<Class> ClassList
        {
            get => _ClassList;
            set => SetProperty(ref _ClassList, value);
        }

        private bool _IsRefreshing = false;
        public bool IsRefreshing
        {
            get { return _IsRefreshing; }
            set
            {
                SetProperty(ref _IsRefreshing, value);
            }
        }

    }
}

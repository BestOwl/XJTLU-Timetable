using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace XJTLU_Timetable_UWP
{
    public class MainPageViewModel : INotifyPropertyChanged
    {

        private string _Username;
        public string Username
        {
            get => _Username;
            set => SetProperty(ref _Username, value);
        }

        private string _CurrentDateDisplay;
        public string CurrentDateDisplay
        {
            get => _CurrentDateDisplay;
            set => SetProperty(ref _CurrentDateDisplay, value);
        }

        private ObservableCollection<Class> _ClassPreviewList = new ObservableCollection<Class>();
        public ObservableCollection<Class> ClassPreviewList
        {
            get => _ClassPreviewList;
            set => SetProperty(ref _ClassPreviewList, value);
        }

        private string _LoginResultDisplay;
        public string LoginResultDisplay
        {
            get => _LoginResultDisplay;
            set => SetProperty(ref _LoginResultDisplay, value);
        }

        private bool _IsUpdating;
        public bool IsUpdating
        {
            get => _IsUpdating;
            set
            {
                SetProperty(ref _IsUpdating, value);
                OnPropertyChanged("IsNotUpdating");
            }
        }

        public bool IsNotUpdating
        {
            get => !IsUpdating;
        }

        private bool _AutoUpdate;
        public bool AutoUpdate
        {
            get => _AutoUpdate;
            set => SetProperty(ref _AutoUpdate, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool SetProperty<T>(ref T property, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(property, value))
            {
                return false;
            }

            property = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Notifies listeners that a property value has changed.
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

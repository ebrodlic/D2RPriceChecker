using D2RPriceChecker.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;

namespace D2RPriceChecker.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private SettingsService _service;

        public bool SaveImagesToDisk
        {
            get => _service.Settings.SaveImagesToDisk;
            set
            {
                if (_service.Settings.SaveImagesToDisk != value)
                {
                    _service.Settings.SaveImagesToDisk = value;    // Single source of truth
                    _service.Save();                               // Persist immediately
                    OnPropertyChanged();                           // Notify UI of change
                }
            }
        }

        public bool AutoCheckForUpdates
        {
            get => _service.Settings.AutoCheckForUpdates;
            set
            {
                if (_service.Settings.AutoCheckForUpdates != value)
                {
                    _service.Settings.AutoCheckForUpdates = value;    // Single source of truth
                    _service.Save();                                   // Persist immediately
                    OnPropertyChanged();                               // Notify UI of change
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public SettingsViewModel(SettingsService service) 
        {
            _service = service;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ROLF.ViewModels
{
    public class NetworkSettingsPageViewModel: INotifyPropertyChanged
    {
        private string _newIPaddress;
        private string _curentIPaddress;
        public INavigation Navigation { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public Command NavigateApply { get; set; }

        public Command NavigateAbort { get; set; }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public NetworkSettingsPageViewModel(INavigation navigation)
        {
            Navigation = navigation;
            NavigateApply = new Command(async () => await ApplyNetworkSettings());
            NavigateAbort = new Command(async () => await AbortNetworkSettings());
            IPAddress = "172.19.103.100";
        }

        public string IPAddress
        {
            get
            {
                return _newIPaddress;
            }
            set
            {
                if (_newIPaddress != value)
                {
                    _newIPaddress = value;
                    OnPropertyChanged("IPAddress");
                }
            }
        }

        private async Task AbortNetworkSettings()
        {
            await Navigation.PopAsync();
        }

        private async Task ApplyNetworkSettings()
        {
            await Navigation.PopAsync();
        }
    }
}


namespace ROLF.Views
{
    using ROLF.ViewModels;
    using Xamarin.Forms;
    public partial class NetworkSettingsPage : ContentPage
    {
        public NetworkSettingsPage()
        {
            InitializeComponent();
            BindingContext = new NetworkSettingsPageViewModel(Navigation);
        }
    }
}

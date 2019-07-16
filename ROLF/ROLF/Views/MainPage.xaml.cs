

namespace ROLF.Views
{
    using ROLF.ViewModels;
    using Xamarin.Forms;
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainPageViewModel(Navigation);
        }

    }

}
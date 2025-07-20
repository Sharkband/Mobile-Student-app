using MobileApp.ViewModels;

namespace MobileApp.Views
{
    public partial class MainPage : ContentPage
    {
    

        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

       
    }
}

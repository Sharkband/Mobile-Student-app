using MobileApp.ViewModels;
namespace MobileApp.Views;

public partial class AIChatPage : ContentPage
{
    
    public AIChatPage(AIChatViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
	}

    
}
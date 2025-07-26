using MobileApp.ViewModels;

namespace MobileApp.Views
{
	public partial class FlashcardPage : ContentPage
	{
		public FlashcardPage(FlashcardViewModel viewModel)
		{
			InitializeComponent();
            BindingContext = viewModel;
        }
	}
}
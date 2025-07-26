using MobileApp.ViewModels;
namespace MobileApp.Views;

public partial class QuizPage : ContentPage
{
	public QuizPage(QuizViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}
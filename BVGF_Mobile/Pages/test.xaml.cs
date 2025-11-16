namespace BVGF.Pages;

public partial class test : ContentPage
{
	public test()
	{
		InitializeComponent();
	}
    private async void OnButtonClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Hello!", "Button clicked successfully!", "OK");
    }
}
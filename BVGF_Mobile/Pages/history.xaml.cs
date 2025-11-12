using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace BVGF.Pages
{
    public partial class history : ContentPage
    {
        public event EventHandler CloseRequested;

        public history()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        // Add methods to load/manage history data
        public void LoadHistoryData()
        {
            // Your data loading logic here
        }
    }
}
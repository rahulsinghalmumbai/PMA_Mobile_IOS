using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace BVGF.CustomControls
{
    public partial class CustomToastControl : ContentView
    {
        public CustomToastControl()
        {
            InitializeComponent();
        }

        public async Task ShowAsync(string message, int duration = 3000)
        {
            ToastLabel.Text = message;
            ToastFrame.IsVisible = true;
            ToastFrame.Opacity = 0;

            try
            {
                await ToastFrame.FadeTo(1, 250); 
                await Task.Delay(duration);      
                await ToastFrame.FadeTo(0, 250); 
            }
            finally
            {
                ToastFrame.IsVisible = false;
            }
        }
    }
}

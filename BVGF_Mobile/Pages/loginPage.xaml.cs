using BVGF.Connection;
using CommunityToolkit.Maui.Media;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace BVGF.Pages
{
    public partial class loginPage : ContentPage
    {
        private readonly ApiService _apiService;
        private readonly SimCardService _simService;

        public loginPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            _apiService = new ApiService();
            _simService = new SimCardService();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await Toast.ShowAsync("Please enter mobile number");

                await DisplayAlert("Error", "Please enter mobile number", "OK");
                return;
            }

            if (PasswordEntry.Text.Length != 10)
            {
                await DisplayAlert("Error", "Please enter a valid 10-digit mobile number", "OK");
                return;
            }

            LoginButton.IsEnabled = false;
            LoginButton.Text = "Verifying...";
            await Task.Delay(100);
            try
            {
                bool hasPermission = await _simService.HasPermissionAsync();
                if (!hasPermission)
                {
                    await DisplayAlert("Permission Required",
                        "Phone permission is required to verify your Credential for security purposes.",
                        "OK");
                    return;
                }

                LoginButton.Text = "Checking Credential...";
                await Task.Delay(100);
                //var verificationResponse = await _simService.VerifyPhoneNumberAsync(PasswordEntry.Text.Trim());
                //if (verificationResponse.Result != VerificationResult.Success)
                //{
                //    await DisplayAlert("Verification Failed",
                //        verificationResponse.Message,
                //        "OK");
                //    return;
                //}

                LoginButton.Text = "Logging in...";
                await Task.Delay(100);

                var isLoginSuccessful = await _apiService.LoginAsync(PasswordEntry.Text.Trim());

                if (isLoginSuccessful != null &&
                   isLoginSuccessful.Status == "Success" &&
                   isLoginSuccessful.Message == "Login Successfully")
                {
                    
                    await SecureStorage.SetAsync("logged_in_mobile", PasswordEntry.Text.Trim());
                    // await DisplayAlert("Success", "Login successful!", "OK");
                    //await Navigation.PushAsync(new homePage());
                    var speechToText = Handler.MauiContext.Services.GetService<ISpeechToText>();
                   
                    await Navigation.PushAsync(new homePage(speechToText));
                     Navigation.RemovePage(this);
                }
                else
                {
                    await DisplayAlert("Login Failed",
                        "Invalid mobile number or login credentials. Please check your number and try again.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");
            }
            finally
            {
               
                LoginButton.IsEnabled = true;
                LoginButton.Text = "LOGIN";
            }
        }

        // Helper method to get available numbers message
        //private async Task<string> GetAvailableNumbersMessage()
        //{
        //    try
        //    {
        //        var simCards = await _simService.GetAllSimCardInfoAsync();
        //        string message = "";

        //        foreach (var sim in simCards)
        //        {
        //            if (!string.IsNullOrEmpty(sim.PhoneNumber))
        //            {
        //                message += $"• {sim.PhoneNumber} ({sim.CarrierName})\n";
        //            }
        //            else
        //            {
        //                message += $"• Number not available ({sim.CarrierName})\n";
        //            }
        //        }

        //        return string.IsNullOrEmpty(message) ? "No SIM numbers available" : message;
        //    }
        //    catch
        //    {
        //        return "Unable to retrieve SIM numbers";
        //    }
        //}

        // Method to show user their SIM numbers
        //private async void OnShowMyNumbersClicked(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        LoadingIndicator.IsVisible = true;
        //        LoadingIndicator.IsRunning = true;

        //        bool hasPermission = await _simService.HasPermissionAsync();
        //        if (!hasPermission)
        //        {
        //            await DisplayAlert("Permission Required",
        //                "Phone permission is required to access your SIM card information.",
        //                "OK");
        //            return;
        //        }

        //        var verification = await _simService.GetSimInfoSummaryAsync();

        //        await DisplayAlert("Your SIM Information", verification, "OK");
        //    }
        //    catch (Exception ex)
        //    {
        //        await DisplayAlert("Error", $"Failed to get SIM information: {ex.Message}", "OK");
        //    }
        //    finally
        //    {
        //        LoadingIndicator.IsVisible = false;
        //        LoadingIndicator.IsRunning = false;
        //    }
        //}

       
        private async void OnForgotPasswordTapped(object sender, EventArgs e)
        {
          
        }

        private async void OnSignUpTapped(object sender, EventArgs e)
        {
           
        }

        protected override bool OnBackButtonPressed()
        {
            
            return base.OnBackButtonPressed();
        }
    }
}
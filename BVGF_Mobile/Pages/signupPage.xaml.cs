using Microsoft.Maui.Controls;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BVGF.Pages
{
    public partial class signupPage : ContentPage
    {
        public signupPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
        }

        private async void OnSignUpClicked(object sender, EventArgs e)
        {
            if (!ValidateInputs())
                return;

            if (!TermsCheckBox.IsChecked)
            {
                await DisplayAlert("Error", "Please accept the Terms and Conditions to continue", "OK");
                return;
            }

            // Show loading indicator
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            SignUpButton.IsEnabled = false;

            try
            {
                // Simulate API call
                await Task.Delay(2000);

                // TODO: Replace with actual registration logic
                bool isRegistrationSuccessful = await RegisterUser();

                if (isRegistrationSuccessful)
                {
                    await DisplayAlert("Success", "Account created successfully!", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Error", "Registration failed. Please try again.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Registration failed: {ex.Message}", "OK");
            }
            finally
            {
                // Hide loading indicator
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                SignUpButton.IsEnabled = true;
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(FullNameEntry.Text))
            {
                DisplayAlert("Error", "Please enter your full name", "OK");
                return false;
            }

            if (string.IsNullOrWhiteSpace(EmailEntry.Text))
            {
                DisplayAlert("Error", "Please enter your email address", "OK");
                return false;
            }

            if (string.IsNullOrWhiteSpace(PhoneEntry.Text))
            {
                DisplayAlert("Error", "Please enter your phone number", "OK");
                return false;
            }

            if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                DisplayAlert("Error", "Please enter a password", "OK");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
            {
                DisplayAlert("Error", "Please confirm your password", "OK");
                return false;
            }

            if (!IsValidEmail(EmailEntry.Text))
            {
                DisplayAlert("Error", "Please enter a valid email address", "OK");
                return false;
            }

            if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
            {
                DisplayAlert("Error", "Passwords do not match", "OK");
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private async void OnLoginTapped(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async Task<bool> RegisterUser()
        {
            // TODO: Replace with actual registration logic
            await Task.Delay(1000);
            return true;
        }
    }
}
using BVGF_Mobile;
using System;
using System.Threading.Tasks;

namespace BVGF.Pages
{
    public partial class SplashPage : ContentPage
    {
        private bool _skipPressed = false;

        public SplashPage()
        {
            InitializeComponent();
            _ = StartAnimationSequence();
        }

        private async Task StartAnimationSequence()
        {
            await Task.Delay(200);

            var logoAnimation = AnimateLogo();
            var contentAnimation = AnimateContent();
            var loadingAnimation = SimulateLoading();

            await Task.WhenAll(logoAnimation, contentAnimation, loadingAnimation);

            if (!_skipPressed)
            {
                await NavigateToMainPage();
            }
        }

        private async Task AnimateLogo()
        {
            await LogoFrame.ScaleTo(1.1, 800, Easing.BounceOut);

            _ = PulseAnimation();

            await LogoFrame.ScaleTo(1.0, 200, Easing.SinOut);
        }

        private async Task PulseAnimation()
        {
            while (!_skipPressed)
            {
                await PulseOverlay.FadeTo(0.3, 1000, Easing.SinInOut);
                await PulseOverlay.FadeTo(0.1, 1000, Easing.SinInOut);
            }
        }

        private async Task AnimateContent()
        {
            // Animate title
            var titleTask = Task.Run(async () =>
            {
                await Task.Delay(400);
                await AppTitle.FadeTo(1, 600, Easing.SinOut);
            });

            var subtitleTask = Task.Run(async () =>
            {
                await Task.Delay(800);
                await SubTitle.FadeTo(1, 600, Easing.SinOut);
            });

            var loadingTask = Task.Run(async () =>
            {
                await Task.Delay(1200);
                await LoadingSection.FadeTo(1, 600, Easing.SinOut);
            });

            var progressTask = Task.Run(async () =>
            {
                await Task.Delay(1400);
                await LoadingProgress.FadeTo(1, 600, Easing.SinOut);
            });

            var skipTask = Task.Run(async () =>
            {
                await Task.Delay(1000);
                await SkipButton.FadeTo(0.7, 600, Easing.SinOut);
            });

            var versionTask = Task.Run(async () =>
            {
                await Task.Delay(1600);
                await VersionLabel.FadeTo(1, 600, Easing.SinOut);
            });

            await Task.WhenAll(titleTask, subtitleTask, loadingTask, progressTask, skipTask, versionTask);
        }

        private async Task SimulateLoading()
        {
            await Task.Delay(1500); 

            for (double i = 0; i <= 1.0 && !_skipPressed; i += 0.05)
            {
                await LoadingProgress.ProgressTo(i, 100, Easing.Linear);

                if (i == 0.3 || i == 0.7)
                {
                    await Task.Delay(300); 
                }
                else
                {
                    await Task.Delay(50);
                }
            }

            if (!_skipPressed)
            {
                await Task.Delay(500);
            }
        }

        private async void OnSkipClicked(object sender, EventArgs e)
        {
            _skipPressed = true;

            await SkipButton.ScaleTo(0.9, 100, Easing.SinOut);
            await SkipButton.ScaleTo(1.0, 100, Easing.SinOut);

            await NavigateToMainPage();
        }

        private async Task NavigateToMainPage()
        {
            try
            {
                //await Navigation.PushAsync(new test());
                await Navigation.PushAsync(new loginPage());
                Navigation.RemovePage(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        protected override bool OnBackButtonPressed()
        {
            return false;
        }
    }
}
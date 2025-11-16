using Foundation;
using UIKit;

namespace BVGF_Mobile
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            // macOS specific initialization
            if (OperatingSystem.IsMacCatalyst())
            {
                // Enable interactions - this method works in .NET 9
                System.Diagnostics.Debug.WriteLine("=== Running on macOS Catalyst ===");
            }

            return base.FinishedLaunching(application, launchOptions);
        }
    }
}
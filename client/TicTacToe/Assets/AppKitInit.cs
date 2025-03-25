using Reown.AppKit.Unity;
using Reown.Core.Common.Logging;
using Reown.Sign.Unity;
using UnityEngine;

public class AppKitInit : MonoBehaviour
{
    private async void Start()
    {
        ReownLogger.Instance = new UnityLogger();

        // AppKit configuration
        var appKitConfig = new AppKitConfig
        {
            // Project ID from https://cloud.reown.com/
            projectId = "7457ffbc1346fad3a828e49743fba2e0",
            metadata = new Metadata(
                "AppKit Unity",
                "AppKit Unity Sample",
                "https://reown.com",
                "https://raw.githubusercontent.com/reown-com/reown-dotnet/main/media/appkit-icon.png",
                new RedirectData
                {
                    // Used by native wallets to redirect back to the app after approving requests
                    Native = "appkit-sample-unity://"
                }
            )
        };

        await AppKit.InitializeAsync(
            appKitConfig
        );
    }

    public void OpenModal()
    {
        AppKit.OpenModal();
    }
}

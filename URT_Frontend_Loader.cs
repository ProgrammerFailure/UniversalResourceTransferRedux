using System.IO;
using UnityEngine;
using KSP.UI.Screens; // Required for the AppLauncher (Stock Toolbar)

namespace UniversalResourceTransferRedux
{
    // ==========================================================================================
    // CLASS 1: THE ASSET LOADER
    // Loads the bundle once when the game starts (Main Menu) so the prefab is ready everywhere.
    // ==========================================================================================
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class URT_Loader : MonoBehaviour
    {
        private static GameObject panelPrefab;
        
        // Public static property so other classes can grab the prefab
        public static GameObject PanelPrefab
        {
            get { return panelPrefab; }
        }

        private void Awake()
        {
            // Safety check: Don't load it twice if KSP reloads the scene
            if (panelPrefab != null) return;

            // 1. Build the path safely using Path.Combine (works on Windows/Mac/Linux)
            string bundlePath = KSPUtil.ApplicationRootPath + "Gamedata/UniversalResourceTransferRedux/Assets/urt_ui";

            // 2. Load the Bundle
            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);

            if (bundle == null)
            {
                Debug.LogError("[URT] CRITICAL: Could not load AssetBundle at " + bundlePath);
                return;
            }

            // 3. Load the Prefab
            // Make sure "URT_Manager_Window_Panel" matches the prefab name in Unity exactly!
            panelPrefab = bundle.LoadAsset("URT_Manager_Window_Panel") as GameObject;

            if (panelPrefab == null)
            {
                Debug.LogError("[URT] CRITICAL: Prefab 'URT_Manager_Window_Panel' not found in bundle!");
            }
            else
            {
                // Prevent the prefab from being destroyed when switching scenes (Flight -> SpaceCenter)
                DontDestroyOnLoad(panelPrefab);
                Debug.Log("[URT] UI Prefab loaded successfully.");
            }

            // 4. Unload the bundle to free up memory (but keep the loaded prefab alive)
            bundle.Unload(false);
        }
    }


    // ==========================================================================================
    // CLASS 2: THE FLIGHT CONTROLLER
    // Handles the Toolbar Button and opening/closing the window in Flight.
    // ==========================================================================================
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class URT_FlightUI : MonoBehaviour
    {
        private ApplicationLauncherButton appButton; // The Toolbar Button
        private GameObject windowInstance;           // The actual window in the scene

        // If you have a specific script on your UI prefab (from your Interface DLL), reference it here:
        // private URT_Frontend.URT_UIManager uiController; 

        private void Start()
        {
            // Register the AppLauncher event
            GameEvents.onGUIApplicationLauncherReady.Add(OnGuiAppLauncherReady);
           
        }

        private void OnDestroy()
        {
            // Clean up events and button when leaving the flight scene
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGuiAppLauncherReady);
            if (appButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(appButton);
            }
            if (windowInstance != null)
            {
                Destroy(windowInstance);
            }
        }

        private void OnGuiAppLauncherReady()
        {
            if (appButton == null)
            {
                 
                appButton = ApplicationLauncher.Instance.AddModApplication(
                    OnToggleOn,     // Clicked ON
                    OnToggleOff,    // Clicked OFF
                    null, null, null, null, // Hover callbacks (ignored)
                    ApplicationLauncher.AppScenes.FLIGHT, // Visible only in flight
                    //GameDatabase.Instance.GetTexture("UniversalResourceTransferRedux/Assets/icon", false) // Update this path to your icon!
                    null as Texture //Uncomment the above line and remove this one once you have an icon. Icon path does not need an extension.
                );
                
            }
        }

        private void OnToggleOn()
        {
            // If the window hasn't been created yet, create it.
            if (windowInstance == null)
            {
                if (URT_Loader.PanelPrefab == null)
                {
                    Debug.LogError("[URT] Tried to open window, but Prefab is null!");
                    return;
                }

                // Instantiate the UI
                windowInstance = Instantiate(URT_Loader.PanelPrefab);

                // Parent it to the KSP Main Canvas so it draws correctly
                windowInstance.transform.SetParent(MainCanvasUtil.MainCanvas.transform, false);

                RectTransform rect = windowInstance.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.localScale = Vector3.one;                  // Forces scale to 1x1x1
                    rect.anchoredPosition = Vector3.zero;           // Centers it on the screen

                    // Optional: If it still looks slightly stretched, force its width/height
                    // rect.sizeDelta = new Vector2(800, 500);      // Change to your actual panel dimensions
                }

                // Optional: Reset position to center of screen
                // windowInstance.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                // Optional: Grab your Interface Script so you can talk to it
                // uiController = windowInstance.GetComponent<URT_Frontend.URT_UIManager>();
            }

            windowInstance.SetActive(true);
            var windowController = windowInstance.GetComponent<URT_Frontend.URT_UIManager>();
            windowController.SetBackendReference(new URT_Frontend_Interface());

            // Generate a fake, invisible, full-screen canvas layer for popups
            GameObject popupObj = new GameObject("URT_PopupLayer");
            popupObj.transform.SetParent(MainCanvasUtil.MainCanvas.transform, false);

            RectTransform popupRect = popupObj.AddComponent<RectTransform>();
            // Stretch it to fill the whole screen
            popupRect.anchorMin = Vector2.zero;
            popupRect.anchorMax = Vector2.one;
            popupRect.offsetMin = Vector2.zero;
            popupRect.offsetMax = Vector2.zero;

            // Tell your UI manager where it is
            windowController.SetPopupLayer(popupRect);
        }

        private void OnToggleOff()
        {
            if (windowInstance != null)
            {
                windowInstance.SetActive(false);
            }
        }


    }
}
using KSP.UI.Screens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UniversalResourceTransferRedux.RegistryComponents;

namespace UniversalResourceTransferRedux
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    internal class URT_FrontendLoader : MonoBehaviour
    {
        static GameObject panelPrefab;
        public static GameObject PanelPrefab
        {
            get
            {
                return panelPrefab;
            }
        }

        private void Awake()
        {
            AssetBundle prefabs = AssetBundle.LoadFromFile(KSPUtil.ApplicationRootPath + "GameData/UniversalResourceTransferRedux/Assets/urt_ui");
            panelPrefab = prefabs.LoadAsset("URT_Manager_Window_Panel") as GameObject;
            if (panelPrefab == null)
            {
                Debug.LogError("[URT]: Critical! URT_Manager_Window_Panel not found in AssetBundle!");
            }
            else
            {
                DontDestroyOnLoad(panelPrefab);
                Debug.Log("[URT]: Window prefab successfully loaded");
            }
            prefabs.Unload(false);
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    internal class URT_WindowInstantiator : MonoBehaviour
    {
        private ApplicationLauncherButton appButton;
        private GameObject windowInstance;
        private URT_Frontend.URT_UIManager windowManager;

        private void Start()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(OnGuiAppLauncherReady);
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
                    null as Texture // Update this path to your icon!
                );
            }
        }

        private void OnToggleOn()
        {
            // If the window hasn't been created yet, create it.
            if (windowInstance == null)
            {
                if (URT_FrontendLoader.PanelPrefab == null)
                {
                    Debug.LogError("[URT] Tried to open window, but Prefab is null!");
                    return;
                }

                // Instantiate the UI
                windowInstance = Instantiate(URT_FrontendLoader.PanelPrefab);

                // Parent it to the KSP Main Canvas so it draws correctly
                windowInstance.transform.SetParent(MainCanvasUtil.MainCanvas.transform, false);
            }
            windowInstance.SetActive(true);
            windowManager = windowInstance.GetComponent<URT_Frontend.URT_UIManager>();

        }

        private IEnumerator WaitForRegistry()
        {
            while (URT_Registry.Instance == null)
            {
                yield return null;
            }
            windowManager.SetBackendReference(new URT_Frontend_Interface());
            URT_Registry.Instance.registerListener(windowManager.ForceRefresh);
        }

        private void OnToggleOff()
        {
            if (windowInstance != null)
            {
                windowInstance.SetActive(false);
            }
        }

        private void OnDisable()
        {
            Destroy(windowInstance);
        }
    }
}

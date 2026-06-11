using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UniversalResourceTransferRedux.Core
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class URT_AssemblyScanner : MonoBehaviour
    {
        public static readonly HashSet<string> CompatibleTransmitters = new HashSet<string>();
        public static readonly HashSet<string> CompatibleReceivers = new HashSet<string>();
        private static bool isScanComplete = false;

        private void Awake()
        {
            DontDestroyOnLoad(this);

            if (!isScanComplete)
            {
                ScanAssemblies();
            }
        }

        private void ScanAssemblies()
        {
#if DEBUG
            Debug.Log("[URT] Starting Assembly Scan for compatible transmitters and receivers...");
#endif

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    Type[] types = assembly.GetTypes();
                    ProcessTypes(types);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    if (ex.Types != null)
                    {
                        ProcessTypes(ex.Types);
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.LogWarning("[URT] Failed to scan assembly: " + assembly.FullName + " - " + ex.Message);
#endif
                }
            }

            isScanComplete = true;

#if DEBUG
            Debug.Log("[URT] Assembly Scan Complete!");
            Debug.Log("[URT] Registered Transmitters: " + CompatibleTransmitters.Count);
            Debug.Log("[URT] Registered Receivers: " + CompatibleReceivers.Count);
#endif
        }

        private void ProcessTypes(Type[] types)
        {
            foreach (Type type in types)
            {
                if (type == null || !type.IsClass || type.IsAbstract)
                {
                    continue;
                }

                if (typeof(IURT_Transmitter).IsAssignableFrom(type))
                {
                    CompatibleTransmitters.Add(type.Name);
#if DEBUG
                    Debug.Log("[URT] Registered compatible transmitter module: " + type.Name);
#endif
                }

                if (typeof(IURT_Receiver).IsAssignableFrom(type))
                {
                    CompatibleReceivers.Add(type.Name);
#if DEBUG
                    Debug.Log("[URT] Registered compatible receiver module: " + type.Name);
#endif
                }
            }
        }
    }
}

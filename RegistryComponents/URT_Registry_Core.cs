using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security.Policy;
using UnityEngine;
using static UniversalResourceTransferRedux.GenericUtils;
using static VehiclePhysics.EnergyProvider;

namespace UniversalResourceTransferRedux.RegistryComponents
{
    // Scenario modules are per game save.
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER)]
    internal partial class URT_Registry : ScenarioModule
    {
        [KSPField(isPersistant = true)]
        int nextTransmitterId = 1;

        [KSPField(isPersistant = true)]
        int nextReceiverId = 1;

        Dictionary<int, uint> transmitterFlightIds = new Dictionary<int, uint>();
        Dictionary<int, uint> receiverFlightIds = new Dictionary<int, uint>();

        Dictionary<int, URT_Receiver> activeReceiverCache = new Dictionary<int, URT_Receiver>();
        Dictionary<int, URT_Transmitter> activeTransmitterCache = new Dictionary<int, URT_Transmitter>();
        

        /*
   Place this refactored code inside your URT_Registry class,
   replacing your existing GetTransmitter and GetReceiverInfo methods.
*/

        #region Generic Data Fetching

        /// <summary>
        /// A single, private, generic method to fetch info for any URT module.
        /// It consolidates the complex logic of checking active caches, finding live parts,
        /// and falling back to proto-part data parsing.
        /// </summary>
        /// <typeparam name="TModule">The PartModule type to look for (e.g., URT_Receiver).</typeparam>
        /// <typeparam name="TInfo">The struct type to return (e.g., ReceiverInfo).</typeparam>
        /// <param name="customId">The unique URT ID of the module.</param>
        /// <param name="flightIdMap">The dictionary mapping URT IDs to KSP part flight IDs.</param>
        /// <param name="activeCache">The dictionary of currently loaded, active module instances.</param>
        /// <param name="moduleNameForProto">The string name of the module, for searching in ProtoPartSnapshots.</param>
        /// <param name="getModuleIdFromInstance">A delegate to retrieve the URT ID from a live module instance.</param>
        /// <param name="getInfoFromLiveModule">A delegate to convert a live module instance into its TInfo struct.</param>
        /// <param name="getInfoFromProtoModule">A delegate to parse a ProtoPartModuleSnapshot into its TInfo struct.</param>
        /// <param name="classNameForLogging">The name of the calling class for logging purposes.</param>
        /// <returns>A nullable TInfo struct containing the module's data, or null if not found.</returns>
        private TInfo? GetModuleInfo<TModule, TInfo>(
            int customId,
            Dictionary<int, uint> flightIdMap,
            Dictionary<int, TModule> activeCache,
            string moduleNameForProto,
            Func<TModule, int> getModuleIdFromInstance,
            Func<TModule, TInfo> getInfoFromLiveModule,
            Func<ProtoPartModuleSnapshot, ProtoVessel, TInfo> getInfoFromProtoModule,
            string classNameForLogging)
            where TModule : PartModule
            where TInfo : struct
        {
            // --- STAGE 0: FAST PATH (Check Active Cache) ---
            // This is the primary optimization. If the module is loaded, we get its data directly.
            if (activeCache.TryGetValue(customId, out TModule cachedModule))
            {
                // Make sure the cached object hasn't been destroyed by Unity somehow (e.g. scene change)
                if (cachedModule != null)
                {
                    return getInfoFromLiveModule(cachedModule);
                }
            }

            // --- STAGE 1: SLOW PATH (ID and ProtoPart Validation) ---
            if (!flightIdMap.TryGetValue(customId, out uint flightId))
            {
                Debug.LogError($"{classNameForLogging}: Unable to fetch module with ID {customId}. Reason: ID not found in its corresponding flight ID dictionary.");
                return null;
            }

            var protoPart = FlightGlobals.FindProtoPartByID(flightId);
            if (protoPart == null)
            {
                Debug.LogError($"{classNameForLogging}: Unable to fetch module with ID {customId}. Reason: ProtoPartSnapshot with flightID {flightId} not found.");
                return null;
            }

            // --- STAGE 2: Try to find the live module instance ---
            // This handles the case where the part is loaded, but wasn't in our active cache for some reason.
            if (protoPart.pVesselRef?.vesselRef?.Parts != null)
            {
                var actualPart = protoPart.pVesselRef.vesselRef.Parts.FirstOrDefault(p => p.flightID == flightId);
                if (actualPart != null)
                {
                    var actualModule = actualPart.FindModulesImplementing<TModule>().FirstOrDefault(m => getModuleIdFromInstance(m) == customId);
                    if (actualModule != null)
                    {
                        // Found the live module, get its info directly and return.
                        return getInfoFromLiveModule(actualModule);
                    }
                }
            }

            // --- STAGE 3: Fallback to parsing ProtoPartSnapshot data ---
            // This is for parts that are on rails or otherwise not loaded.
            var protoModuleData = protoPart.modules
                .Where(s => s.moduleName == moduleNameForProto)
                .Where(s => s.moduleValues != null && s.moduleValues.HasValue("transmitterId") || s.moduleValues.HasValue("receiverId"))
                .FirstOrDefault(s => s.moduleValues.GetInt(moduleNameForProto == "URT_Transmitter" ? "transmitterId" : "receiverId") == customId);

            if (protoModuleData != null)
            {
                // Use the provided delegate to parse the proto data into the required info struct.
                return getInfoFromProtoModule(protoModuleData, protoPart.pVesselRef);
            }

            // --- FINAL FAILURE ---
            Debug.LogError($"{classNameForLogging}: All attempts to fetch module with ID {customId} have failed. No live module or valid proto data found.");
            return null;
        }

        

        #endregion
    }
}
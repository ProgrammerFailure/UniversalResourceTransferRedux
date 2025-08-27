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
    }
}
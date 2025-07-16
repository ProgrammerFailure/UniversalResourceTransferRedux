using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace UniversalResourceTransferRedux
{
    internal class URT_PowerCalculator
    {
        URT_Registry registry = ScenarioRunner.GetLoadedModules().Find(s => s.ClassName == "URT_Registry") as URT_Registry;



        public void CalculateAndSetRecvPower(int receiverId)
        {
            List < (ProtoPartModuleSnapshot transmitterModule, float sentPower)> pairedTransmitters = new List<(ProtoPartModuleSnapshot, float)>();
            ProtoPartModuleSnapshot receiverModuleSnapshot = registry.GetReceiverProtoPartModuleByReceiverId(receiverId);
            URT_Receiver receiverFullModule;
            bool receiverIsActive = false;
            if (receiverModuleSnapshot.moduleRef.vessel.isActiveVessel) { receiverFullModule = receiverModuleSnapshot.moduleRef as URT_Receiver; receiverIsActive = true; }

            foreach ((int currentTransmitterId, double recvPower) in registry.GetReceiverPairings(receiverId))
            {                 
                ProtoPartModuleSnapshot currentTransmitterModule = registry.GetTransmitterProtoPartModuleByTransmitterId(currentTransmitterId);    
            }

            if (receiverIsActive) //receiver is on the active vessel
            {
                 float receiverArea;
                 float receiverWavelength;
                 float receiverEfficiency;
    }   
        }
    }
}

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
            
            foreach ((int currentTransmitterId, double recvPower) in registry.GetReceiverPairings(receiverId))
            {
                if ()
                
                
                ProtoPartModuleSnapshot currentTransmitterModule = registry.GetTransmitterProtoPartModuleByTransmitterId(currentTransmitterId);
                
            }
        }
    }
}

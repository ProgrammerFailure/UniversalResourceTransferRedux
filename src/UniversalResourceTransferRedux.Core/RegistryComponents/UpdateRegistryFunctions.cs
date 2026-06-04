using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalResourceTransferRedux.Core.RegistryComponents
{
    partial class URT_Registry
    {
        public void OnTransmitterMaxPowerChanged(int transmitterId, float newMaxPower)
        {
            transmitterCurrentMaxAmounts[transmitterId] = newMaxPower;
            RebuildLinks();
        }
        public void OnReceiverRequestedAmountChanged(int receiverId, float newRequestedPower)
        {
            receiverRequestedAmounts[receiverId] = newRequestedPower;
            RebuildLinks();
        }
        public void RegisterManualTransmitter(int transmitterId, int receiverId)
        {
            manualTransmittersToTargets[transmitterId] = receiverId;
            RebuildLinks();
        }
        public void RegisterReservedForActiveVesselTransmitter(int transmitterId)
        {
            reservedForActiveVesselTransmitters.Add(transmitterId);
            RebuildLinks();
        }
        public void DeregisterManualTransmitter(int transmitterId)
        {
            manualTransmittersToTargets.Remove(transmitterId);
            RebuildLinks();
        }
        public void DeregisterReservedForActiveVesselTransmitter(int transmitterId)
        {
            reservedForActiveVesselTransmitters.Remove(transmitterId);
            RebuildLinks();
        }
        
    }
}

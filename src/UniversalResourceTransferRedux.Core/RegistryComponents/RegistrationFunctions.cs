using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UniversalResourceTransferRedux.Core.RegistryComponents
{
    internal partial class URT_Registry
    {
        public int RegisterNewTransmitter(uint partFlightId, IURT_Transmitter transmitterModule, GenericUtils.TransmitterInfo txInfo, int transmitterModuleId)
        {
            if (transmitterFlightIds.ContainsValue(partFlightId) && transmitterModuleIds.ContainsValue(transmitterModuleId))
            {
                throw new InvalidOperationException("Duplicate transmitter added!");
            }
            var assignedId = nextTransmitterInt;
            nextTransmitterInt++;
            transmitterFlightIds[assignedId] = partFlightId;
            transmitterModuleIds[assignedId] = transmitterModuleId;
            activeTransmitterCache[assignedId] = transmitterModule;
            inactiveTransmitterCache.Remove(assignedId);
            foreach (var receiverId in receiverFlightIds.Keys)
            {
                RefreshCacheReceiver(receiverId);
                var link = CreateLink(txInfo, GetReceiverInfo(receiverId), assignedId, receiverId);
                if (link != null && !Links.Contains(link))
                {
                    Links.Add(link);
                }
#if DEBUG
                else if (link == null)
                {

                    Debug.Log("[URT]: Link is null!");
                }
                else if (Links.Contains(link))
                {
                    Debug.Log("[URT]: Links already contains link!");
                }
#endif
            }
            
            return assignedId;
        }

        public int RegisterNewReceiver(uint partFlightId, IURT_Receiver receiverModule, GenericUtils.ReceiverInfo rxInfo, int receiverModuleId)
        {
            if (receiverFlightIds.ContainsValue(partFlightId) && receiverModuleIds.ContainsValue(receiverModuleId))
            {
                throw new InvalidOperationException("Duplicate receiver added!");
            }
            var assignedId = nextReceiverId;
            receiverFlightIds.Add(assignedId, partFlightId);
            receiverModuleIds.Add(assignedId, receiverModuleId);
            activeReceiverCache[assignedId] = receiverModule;
            inactiveReceiverCache.Remove(assignedId);
            nextReceiverId++;
            foreach (var transmitterId in transmitterFlightIds.Keys)
            {
                var link = CreateLink(GetTransmitterInfo(transmitterId), rxInfo, transmitterId, assignedId);
                if (link != null && !Links.Contains(link))
                {
                    Links.Add(link);
                }
#if DEBUG
                else if (link == null)
                {

                    Debug.Log("[URT]: Link is null!");
                }
                else if (Links.Contains(link))
                {
                    Debug.Log("[URT]: Links already contains link!");
                }
#endif
            }

            CallAllListeners();
            return assignedId;
        }

        public void RegisterActiveTransmitter(int transmitterId, IURT_Transmitter transmitterModule)
        {
            if (!transmitterFlightIds.ContainsKey(transmitterId)) return;
            activeTransmitterCache[transmitterId] = transmitterModule;
        }
        public void RegisterActiveReceiver(int receiverId, IURT_Receiver receiverModule)
        {
            if (!receiverFlightIds.ContainsKey(receiverId)) return;
            activeReceiverCache[receiverId] = receiverModule;
        }
        public void DeregisterActiveTransmitter(int transmitterId)
        {
            activeTransmitterCache.Remove(transmitterId);
        }
        public void DeregisterActiveReceiver(int receiverId)
        {
            activeReceiverCache.Remove(receiverId);
        }

        public void RegisterListener(Action listener)
        {
            listeners.Add(listener);
        }
    }
}

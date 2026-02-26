using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static UniversalResourceTransferRedux.GenericUtils;

namespace UniversalResourceTransferRedux.RegistryComponents
{
    internal partial class URT_Registry
    {
        public void registerListener(Action listener)
        {
            registryEventListeners.Add(listener);
        }
        public int registerNewReceiverId(uint partFlightId)
        {
            var receiverId = nextReceiverId;
            nextReceiverId += 1;
            receiverFlightIds.Add(receiverId, partFlightId);
            callListeners();
            return receiverId;
        }

        public int registerNewTransmitterId(uint partFlightId)
        {
            var transmitterId = nextTransmitterId;
            nextTransmitterId += 1;
            transmitterFlightIds.Add(transmitterId, partFlightId);
            callListeners();
            return transmitterId;
        }

        public void registerActiveReceiver(int receiverId, URT_Receiver receiverObject)
        {
            if (!receiverFlightIds.Keys.Contains(receiverId))
            {
                activeReceiverCache.Add(receiverId, receiverObject);
            }
        }

        public void deregisterActiveReceiver(int receiverId)
        {
            if (activeReceiverCache.Keys.Contains(receiverId))
            {
                activeReceiverCache.Remove(receiverId);
            }
        }

        public void registerActiveTransmitter(int transmitterId, URT_Transmitter transmitterObject)
        {
            if (!activeTransmitterCache.ContainsKey(transmitterId))
            {
                activeTransmitterCache.Add(transmitterId, transmitterObject);
            }
        }

        public void deregisterActiveTransmitter(int transmitterId)
        {
            if (activeTransmitterCache.ContainsKey(transmitterId))
            {
                activeTransmitterCache.Remove(transmitterId);
            }
        }
        public uint getTransmitterPartFlightIdById(int transmitterId)
        {
            return transmitterFlightIds[transmitterId];
        }

        public uint getReceiverPartFlightIdById(int receiverId)
        {
            return receiverFlightIds[receiverId];
        }

        public void deregisterReceiver(int receiverId)
        {
            receiverFlightIds.Remove(receiverId);
            callListeners();
        }

        public void deregisterTransmitter(int transmitterId)
        {
            transmitterFlightIds.Remove(transmitterId);
            callListeners();
        }

        public Dictionary<int, uint> getReceiverIds()
        {
            return receiverFlightIds;
        }

        public List<ReceiverInfo> FetchAllReceivers(int transmitterID, string className)
        {
            List<ReceiverInfo> output = new List<ReceiverInfo>();
            foreach (int receiverId in receiverFlightIds.Keys)
            {
                var receiverInfo = GetReceiverInfo(receiverId);
                if (receiverInfo != null)
                {
                    output.Add(receiverInfo.Value);
                }
            }
            return output;
        }
    }  
}

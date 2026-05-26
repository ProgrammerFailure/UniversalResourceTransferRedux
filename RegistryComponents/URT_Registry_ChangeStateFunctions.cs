using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UniversalResourceTransferRedux.RegistryComponents
{
    internal partial class URT_Registry
    {
        public void SetTransmitterState(int transmitterId, bool isEnabled)
        {
            if (activeTransmitterCache.TryGetValue(transmitterId, out URT_Transmitter transmitter) && transmitter != null)
            {
                transmitter.SetTransmitterState(isEnabled);
            }
            else if (transmitterFlightIds.TryGetValue(transmitterId, out uint flightId))
            {
                if (FlightGlobals.FindPartByID(flightId) is Part part)
                {
                    part.FindModulesImplementing<URT_Transmitter>()
                        .Find(s => s.transmitterID == transmitterId)
                        ?.SetTransmitterState(isEnabled);
                }
                else if (FlightGlobals.FindProtoPartByID(flightId) is ProtoPartSnapshot protoPart)
                {
                    var mod = protoPart.modules.Find(s => s.moduleName == "URT_Transmitter" && s.moduleValues.HasValue("transmitterID") && s.moduleValues.GetValue("transmitterID") == transmitterId.ToString());
                    mod?.moduleValues.SetValue("isTransmitting", isEnabled.ToString(), true);
                }
            }

            TriggerAllListeners();
        }

        public void SetReceiverState(int receiverId, bool isEnabled)
        {
            Debug.Log($"[URT]: SetReceiverState called with value: {isEnabled} and receiverId: {receiverId}");
            if (activeReceiverCache.TryGetValue(receiverId, out URT_Receiver receiver) && receiver != null)
            {
                receiver.SetReceiverState(isEnabled);
            }
            else if (receiverFlightIds.TryGetValue(receiverId, out uint flightId))
            {
                if (FlightGlobals.FindPartByID(flightId) is Part part)
                {
                    part.FindModulesImplementing<URT_Receiver>()
                        .Find(s => s.receiverID == receiverId)
                        ?.SetReceiverState(isEnabled);
                }
                else if (FlightGlobals.FindProtoPartByID(flightId) is ProtoPartSnapshot protoPart)
                {
                    var mod = protoPart.modules.Find(s => s.moduleName == "URT_Receiver" && s.moduleValues.GetInt("receiverID", -1) == receiverId);

                    mod?.moduleValues.SetValue("isReceiving", isEnabled.ToString(), true);
                }
            }

            TriggerAllListeners();
        }
        public void SetTransmitterTarget(int transmitterId, int receiverId)
        {
            int oldReceiverId = Instance.GetTransmitterTarget(transmitterId);

            if (activeTransmitterCache.TryGetValue(transmitterId, out URT_Transmitter transmitter) && transmitter != null)
            {
                transmitter.SetTarget(receiverId);
            }
            else if (transmitterFlightIds.TryGetValue(transmitterId, out uint transmitterFlightId))
            {
                if (FlightGlobals.FindPartByID(transmitterFlightId) is Part part)
                {
                    part.FindModulesImplementing<URT_Transmitter>()
                        .Find(s => s.transmitterID == transmitterId)
                        ?.SetTarget(receiverId);
                }
                else if (FlightGlobals.FindProtoPartByID(transmitterFlightId) is ProtoPartSnapshot protoPart)
                {
                    var mod = protoPart.modules.Find(s => s.moduleName == "URT_Transmitter" && s.moduleValues.HasValue("transmitterID") && s.moduleValues.GetValue("transmitterID") == transmitterId.ToString());

                    if (mod != null)
                    {
                        mod.moduleValues.SetValue("targetId", receiverId.ToString(), true);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[URT] Could not find Transmitter {transmitterId} to set target.");
                return;
            }

            if (oldReceiverId != -1)
            {
                if (activeReceiverCache.TryGetValue(oldReceiverId, out URT_Receiver oldReceiver) && oldReceiver != null)
                {
                    oldReceiver.RemoveTransmitter(transmitterId);
                }
                else if (receiverFlightIds.TryGetValue(oldReceiverId, out uint oldReceiverFlightId))
                {
                    if (FlightGlobals.FindPartByID(oldReceiverFlightId) is Part oldReceiverPart)
                    {
                        oldReceiverPart.FindModulesImplementing<URT_Receiver>()
                            .Find(s => s.receiverID == oldReceiverId)
                            ?.RemoveTransmitter(transmitterId);
                    }
                    else if (FlightGlobals.FindProtoPartByID(oldReceiverFlightId) is ProtoPartSnapshot oldReceiverProtoPart)
                    {
                        var moduleSnapshot = oldReceiverProtoPart.modules.Find(m => m.moduleName == "URT_Receiver" && m.moduleValues.HasValue("receiverID") && m.moduleValues.GetValue("receiverID") == oldReceiverId.ToString());

                        if (moduleSnapshot?.moduleValues != null)
                        {
                            string raw = moduleSnapshot.moduleValues.GetValue("pairedTransmitters");
                            if (!string.IsNullOrEmpty(raw))
                            {
                                var transmitters = raw.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                                if (transmitters.Remove(transmitterId.ToString()))
                                {
                                    moduleSnapshot.moduleValues.SetValue("pairedTransmitters", string.Join(",", transmitters), true);
                                }
                            }
                        }
                    }
                }
            }
            if (receiverId != -1)
            {
                if (activeReceiverCache.TryGetValue(receiverId, out URT_Receiver receiver) && receiver != null)
                {
                    receiver.AddTransmitter(transmitterId);
                }
                else if (receiverFlightIds.TryGetValue(receiverId, out uint receiverFlightId))
                {
                    if (FlightGlobals.FindPartByID(receiverFlightId) is Part receiverPart)
                    {
                        receiverPart.FindModulesImplementing<URT_Receiver>()
                            .Find(s => s.receiverID == receiverId)
                            ?.AddTransmitter(transmitterId);
                    }
                    else if (FlightGlobals.FindProtoPartByID(receiverFlightId) is ProtoPartSnapshot receiverProtoPart)
                    {
                        var moduleSnapshot = receiverProtoPart.modules.Find(s => s.moduleName == "URT_Receiver" && s.moduleValues.HasValue("receiverID") && s.moduleValues.GetValue("receiverID") == receiverId.ToString());

                        if (moduleSnapshot?.moduleValues != null)
                        {
                            string raw = moduleSnapshot.moduleValues.GetValue("pairedTransmitters");


                            List<string> transmitters = string.IsNullOrEmpty(raw)
                                ? new List<string>()
                                : raw.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                            if (!transmitters.Contains(transmitterId.ToString()))
                            {
                                transmitters.Add(transmitterId.ToString());
                                moduleSnapshot.moduleValues.SetValue("pairedTransmitters", string.Join(",", transmitters), true);
                            }
                        }
                    }
                }
            }

            TriggerAllListeners();
        }

    }
}

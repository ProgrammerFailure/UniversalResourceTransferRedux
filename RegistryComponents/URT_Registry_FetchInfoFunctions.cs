using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UniversalResourceTransferRedux.GenericUtils;

namespace UniversalResourceTransferRedux.RegistryComponents
{
    internal partial class URT_Registry
    {
        public TransmitterInfo? GetTransmitter(int transmitterId, string className)
        {
            return GetModuleInfo<URT_Transmitter, TransmitterInfo>(
                customId: transmitterId,
                flightIdMap: transmitterFlightIds,
                activeCache: activeTransmitterCache,
                moduleNameForProto: "URT_Transmitter",
                getModuleIdFromInstance: module => module.transmitterID,
                getInfoFromLiveModule: liveModule => liveModule.GetTransmitterInfo(),
                getInfoFromProtoModule: (protoModule, protoVessel) =>
                {
                    // This is the specific logic for parsing a TRANSMITTER from a proto snapshot
                    return new TransmitterInfo(
                        protoModule.moduleValues.GetFloat("transmitterArea", 0f),
                        protoModule.moduleValues.GetFloat("transmitterWavelength", 0f),
                        protoModule.moduleValues.GetFloat("transmitterEfficiency", 0f),
                        protoVessel,
                        protoModule.moduleValues.GetFloat("transmittedPower", 0f),
                        protoModule.moduleValues.GetBool("isTransmitting", false)
                    );
                },
                classNameForLogging: className
            );
        }

        public ReceiverInfo? GetReceiverInfo(int receiverId, string className)
        {
            return GetModuleInfo<URT_Receiver, ReceiverInfo>(
                customId: receiverId,
                flightIdMap: receiverFlightIds,
                activeCache: activeReceiverCache,
                moduleNameForProto: "URT_Receiver",
                getModuleIdFromInstance: module => module.receiverID,
                getInfoFromLiveModule: liveModule => liveModule.GetReceiverInfo(),
                getInfoFromProtoModule: (protoModule, protoVessel) =>
                {
                    // This is the specific logic for parsing a RECEIVER from a proto snapshot
                    var info = new ReceiverInfo();
                    info.Area = protoModule.moduleValues.GetFloat("receiverArea", 0f);
                    info.Efficiency = protoModule.moduleValues.GetFloat("receiverEfficiency", 0f);
                    info.Wavelength = protoModule.moduleValues.GetFloat("receiverWavelength", 0f);
                    info.parentProtoVessel = protoVessel;

                    // Handle parsing the list of paired transmitters from the serialized string
                    var serializedList = protoModule.moduleValues.GetString("pairedTransmittersSerialized", "");
                    if (!string.IsNullOrEmpty(serializedList))
                    {
                        info.pairedTransmitters = serializedList.Split(',').Select(int.Parse).ToList();
                    }
                    else
                    {
                        info.pairedTransmitters = new List<int>();
                    }
                    return info;
                },
                classNameForLogging: className
            );
        }
    }
}

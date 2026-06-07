using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalResourceTransferRedux.Core
{
    internal interface IURT_Transmitter
    {
        int TransmitterID { get; }
        int ModuleID { get; }

        Vessel Vessel { get; }

        public GenericUtils.TransmitterInfo GetTransmitterInfo();

    }
}

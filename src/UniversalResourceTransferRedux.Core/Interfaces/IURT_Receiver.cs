using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalResourceTransferRedux.Core
{
    internal interface IURT_Receiver
    {
        int ModuleId { get; }
        int ReceiverId { get; }
        Vessel Vessel { get; }

        GenericUtils.ReceiverInfo GetReceiverInfo();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalResourceTransferRedux.RegistryComponents
{
    internal partial class URT_Registry
    {
        public void TriggerAllListeners()
        {
            foreach (var listener in registryEventListeners)
            {
                listener.Invoke();
            }
        }
    }
}

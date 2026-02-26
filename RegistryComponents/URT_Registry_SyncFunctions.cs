using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalResourceTransferRedux.RegistryComponents
{
    internal partial class URT_Registry
    {
        private void callListeners()
        {
            foreach (var listener in registryEventListeners)
            {
                listener.Invoke();
            }
        }
    }
}

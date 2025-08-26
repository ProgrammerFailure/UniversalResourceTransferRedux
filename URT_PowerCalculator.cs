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

        }
    }
}

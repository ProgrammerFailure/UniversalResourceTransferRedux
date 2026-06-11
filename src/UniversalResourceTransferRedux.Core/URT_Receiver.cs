using System;
using System.Collections;
using UnityEngine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalResourceTransferRedux.Core.RegistryComponents;

namespace UniversalResourceTransferRedux.Core
{
    public class URT_Receiver : PartModule, IURT_Receiver
    {
        //Interface properties
        public int ModuleId { get { return receiverModuleId; } }
        public int ReceiverId { get { return receiverId; } }
        
        public Vessel Vessel => vessel;
        // Part properties
        [KSPField(isPersistant = false, guiActive = false)]
        public float receiverDiameter;
        [KSPField(isPersistant = false, guiActive = true)]
        public int receiverModuleId;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Wavelength")]
        public float receiverWavelength;
        [KSPField(isPersistant = false, guiActive = false)]
        public float receiverEfficiency;
        [KSPField(isPersistant = false, guiActive = false)]
        public double receiverTuningFactor;
        [KSPField(isPersistant = false, guiActive = false)]
        public string outputResource = "ElectricCharge";
        [KSPField(isPersistant = false, guiActive = false)]
        public float outputResourceEnergyFactor = 1.0f; //This is defined as how many EC one unit of output resource is worth
        [KSPField(isPersistant = false, guiActive = false)]
        public string resourceTypeTags = "EMRadiation";
        //Dynamic values
        [KSPField(isPersistant = true, guiActive = false)]
        public int receiverId = -1;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Requested Power")]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.Editor, minValue = 0, maxValue = 100000, stepIncrement = 1)]
        public float requestedPowerGUI;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Receiving State")]
        [UI_Toggle(affectSymCounterparts = UI_Scene.Editor, enabledText = "Receiving Power", disabledText = "Receiving Disabled")]
        public bool isReceiving;
        
        [KSPField(isPersistant = true, guiActive = false)]
        public double lastUpdateTime;

        //Variables
        private URT_Registry registry;
        private int outputResourceHash;
        private double currentReceivedAmount;

        public void Start()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;
            StartCoroutine(WaitForRegistry());
        }
        private IEnumerator WaitForRegistry()
        {
            yield return null;
            while (URT_Registry.Instance == null) yield return null;
            registry = URT_Registry.Instance;
            InitReceiver();
        }
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
        }

        private void InitReceiver()
        {
            if (!HighLogic.LoadedSceneIsFlight) { return; }
            if (receiverId == -1)
            {
                try
                {
                    receiverId = registry.RegisterNewReceiver(part.flightID, this, GetReceiverInfo(), receiverModuleId);

                }
                catch (InvalidOperationException ex)
                {
                    Debug.LogError($"[URT]: Could not register receiver! Exception: {ex}");
                    isReceiving = false;
                    this.moduleIsEnabled = false;
                    return;
                }
            }
            var resourceDef = PartResourceLibrary.Instance.GetDefinition(outputResource);
            if (resourceDef == null)
            {
                Debug.LogError($"[URT]: Could not resolve receiver output resource! Resource name: {outputResource}, receiverId: {receiverId}");
                isReceiving = false;
                this.moduleIsEnabled = false;
                return;
            }
            outputResourceHash = resourceDef.id;
            registry.RegisterActiveReceiver(receiverId, this);
            OnReceiverStateChanged(null, null);

            Fields["requestedPowerGUI"].uiControlFlight.onFieldChanged = OnReceiverStateChanged;
            Fields["isReceiving"].uiControlFlight.onFieldChanged = OnReceiverStateChanged;
            Fields["requestedPowerGUI"].guiUnits = resourceDef.abbreviation + "/s";
            #if DEBUG
            Debug.Log($"[URT]: Receiver with ID {receiverId} fully initialized and ready!");
            #endif
        }

        public void FixedUpdate()
        {
            if (registry == null || !isReceiving || requestedPowerGUI == 0) return;
            vessel.GetConnectedResourceTotals(outputResourceHash,
                 out double vesselCurrentResourceAmount,
                 out double vesselCurrentResourceMaxAmount
            );
            if (vesselCurrentResourceMaxAmount <= 0) return;
            var currentTime = Planetarium.GetUniversalTime();
            if (lastUpdateTime == 0)
            {
                lastUpdateTime = currentTime - (1.0 / 60.0); //Assume one frame, just for convenience
            }
            var deltaTime = currentTime - lastUpdateTime;
            lastUpdateTime = currentTime;

            var amountBeingReceived = (registry.receiverReceivedAmounts[receiverId] / outputResourceEnergyFactor) * deltaTime;


            var spareCapacity = vesselCurrentResourceMaxAmount - vesselCurrentResourceAmount;
            if (amountBeingReceived > spareCapacity)
            {
                isReceiving = false;
                OnReceiverStateChanged(null, null);
                return;
            }
            part.RequestResource(outputResourceHash, -amountBeingReceived);
        }
        #if DEBUG
        //USer functions
        [KSPEvent(active = true, guiActive = true, guiActiveEditor = false, guiName = "Debug dump")]
        private void DebugDump()
        {
            registry.DebugDumpRegistryState();
        }

        [KSPEvent(active = true, guiActive = true, guiActiveEditor = false, guiName = "Run network rebuild")]
        private void RunNetworkRebuild()
        {
            registry.RebuildLinks();
        }
        #endif

        //Internal use functions
        public GenericUtils.ReceiverInfo GetReceiverInfo()
        {
#if DEBUG
            Debug.Log($"[URT] GetReceiverInfo called. Part current values: diameter: {receiverDiameter}, wavelength: {receiverWavelength}, efficiency: {receiverEfficiency}, tuningFactor: {receiverTuningFactor}, resource type tags: {resourceTypeTags}");
#endif
            return new GenericUtils.ReceiverInfo(receiverDiameter, receiverWavelength, receiverEfficiency, receiverTuningFactor, resourceTypeTags.Split(';'));
        }

        private void OnReceiverStateChanged(BaseField field, object obj)
        {
            if (!isReceiving)
            {
                registry.OnReceiverRequestedAmountChanged(receiverId, 0);
                return;
            }
            registry.OnReceiverRequestedAmountChanged(receiverId, requestedPowerGUI * outputResourceEnergyFactor);
        }
    }
}

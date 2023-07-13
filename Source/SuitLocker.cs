using HarmonyLib;
using UnityEngine;
using KSerialization;
using System.Reflection;

// If a duplicant drops a suit or does not return (left planetoid, returned
// using a different path), the dock normally stays empty. Trigger
// delivery again after a sufficiently long time.
namespace AutoSuitDelivery
{
    public class SuitLockerAutoDelivery : KMonoBehaviour, ISim4000ms
    {
        [Serialize]
        public bool deliveryEnabled = false;

        [Serialize]
        public float timeLastHaveSuit = 0; // in seconds

        public SuitMarker suitMarker = null;

        private static FieldInfo onlyTraverseIfUnequipAvailable
            = AccessTools.Field( typeof( SuitMarker ), "onlyTraverseIfUnequipAvailable" );

        public static bool IsApplicableLocker(SuitLocker locker, SuitMarker marker)
        {
            if(!locker.GetComponent<SuitLockerAutoDelivery>().deliveryEnabled)
                return false;
            if(!locker.smi.sm.isConfigured.Get(locker.smi))
                return false;
            if(marker != null && (bool)onlyTraverseIfUnequipAvailable.GetValue( marker ))
                return false;
            return true;
        }

        public void Sim4000ms(float dt)
        {
            SuitLocker locker = GetComponent<SuitLocker>();
            if(!IsApplicableLocker(locker, suitMarker))
                return;
            if(locker.smi.sm.isWaitingForSuit.Get(locker.smi))
                return; // already waiting
            if(locker.GetStoredOutfit() != null)
                return; // it has a suit
            if(GameClock.Instance.GetTime() > timeLastHaveSuit + Options.Instance.DeliveryAfterTime)
                locker.ConfigRequestSuit();
        }
    }

    [HarmonyPatch(typeof(SuitLocker))]
    public class SuitLocker_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ConfigRequestSuit))]
        public static void ConfigRequestSuit(SuitLocker __instance)
        {
            SuitLockerAutoDelivery delivery = __instance.GetComponent<SuitLockerAutoDelivery>();
            if( delivery == null )
                return; // huh? probably something modded
            delivery.deliveryEnabled = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ConfigNoSuit))]
        public static void ConfigNoSuit(SuitLocker __instance)
        {
            SuitLockerAutoDelivery delivery = __instance.GetComponent<SuitLockerAutoDelivery>();
            if( delivery == null )
                return;
            delivery.deliveryEnabled = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(DropSuit))]
        public static void DropSuit(SuitLocker __instance)
        {
            SuitLockerAutoDelivery delivery = __instance.GetComponent<SuitLockerAutoDelivery>();
            if( delivery == null )
                return;
            delivery.deliveryEnabled = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(EquipTo))]
        public static void EquipTo(SuitLocker __instance, Equipment equipment)
        {
            KPrefabID storedOutfit = __instance.GetStoredOutfit();
            if (storedOutfit != null)
            { // This should be called when a dupe is equiping the suit.
            SuitLockerAutoDelivery delivery = __instance.GetComponent<SuitLockerAutoDelivery>();
            if( delivery == null )
                return;
            delivery.timeLastHaveSuit = GameClock.Instance.GetTime();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SetSuitMarker))]
        public static void SetSuitMarker(SuitLocker __instance, SuitMarker suit_marker)
        {
            SuitLockerAutoDelivery delivery = __instance.GetComponent<SuitLockerAutoDelivery>();
            if( delivery == null )
                return;
            delivery.suitMarker = suit_marker;
        }
    }

    [HarmonyPatch(typeof(SuitLockerConfig))]
    public class SuitLockerConfig_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<SuitLockerAutoDelivery>();
        }
    }

    [HarmonyPatch(typeof(JetSuitLockerConfig))]
    public class JetSuitLockerConfig_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<SuitLockerAutoDelivery>();
        }
    }

    [HarmonyPatch(typeof(LeadSuitLockerConfig))]
    public class LeadSuitLockerConfig_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<SuitLockerAutoDelivery>();
        }
    }

    [HarmonyPatch(typeof(OxygenMaskLockerConfig))]
    public class OxygenMaskLockerConfig_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(DoPostConfigureComplete))]
        public static void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<SuitLockerAutoDelivery>();
        }
    }
}

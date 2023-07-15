using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

// Disable the 'No Docks available' notification when a there's no empty suit locker
// when a duplicant returns. Disable only for checkpoints where we resupply suits.
namespace AutoSuitDelivery
{
    public class SuitMarker_UnequipSuitReactable_Patch
    {
        private static readonly Type SuitMarker_UnequipSuitReactable_type = AccessTools.TypeByName(
            "SuitMarker+UnequipSuitReactable" );

        public static void Patch( Harmony harmony )
        {
            MethodInfo info = AccessTools.Method( SuitMarker_UnequipSuitReactable_type, "Run" );
            if( info != null )
                harmony.Patch( info, transpiler: new HarmonyMethod(
                    typeof( SuitMarker_UnequipSuitReactable_Patch ).GetMethod( nameof( Run ))));
        }

        public static IEnumerable<CodeInstruction> Run(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found = false;
            for( int i = 0; i < codes.Count; ++i )
            {
//                Debug.Log("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                // The function has code:
                // if (assignable != null)
                // {
                //     assignable.Unassign();
                //     Notification notification = new Notification(MISC.NOTIFICATIONS.SUIT_DROPPED.NAME, ... );
                // Change to:
                // if (assignable != null)
                // {
                //     assignable.Unassign();
                //     if(Run_Hook(suitMarker))
                //     {
                //         Notification notification = new Notification(MISC.NOTIFICATIONS.SUIT_DROPPED.NAME, ... );
                if( codes[ i ].opcode == OpCodes.Brfalse_S
                    && i + 3 < codes.Count
                    && codes[ i + 1 ].IsLdloc()
                    && codes[ i + 2 ].opcode == OpCodes.Callvirt
                    && codes[ i + 2 ].operand.ToString() == "Void Unassign()"
                    && codes[ i + 3 ].opcode == OpCodes.Ldsfld
                    && codes[ i + 3 ].operand.ToString() == "LocString NAME" )
                {
                    codes.Insert( i + 3, new CodeInstruction( OpCodes.Ldarg_0 )); // load 'this'
                    codes.Insert( i + 4, CodeInstruction.LoadField( SuitMarker_UnequipSuitReactable_type, "suitMarker" ));
                    codes.Insert( i + 5, new CodeInstruction( OpCodes.Call,
                        typeof( SuitMarker_UnequipSuitReactable_Patch ).GetMethod( nameof( Run_Hook ))));
                    codes.Insert( i + 6, codes[ i ].Clone()); // copy the Brfalse_S
                    found = true;
                    break;
                }
            }
            if(!found)
                Debug.LogWarning("AutoSuitDelivery: Failed to patch SuitMarker.UnequipSuitReactable.Run()");
            return codes;
        }

        public static bool Run_Hook(SuitMarker suitMarker)
        {
            return AllowNotification( suitMarker );
        }

        private static FieldInfo onlyTraverseIfUnequipAvailable
            = AccessTools.Field( typeof( SuitMarker ), "onlyTraverseIfUnequipAvailable" );

        public static bool AllowNotification( SuitMarker suitMarker )
        {
            ListPool<SuitLocker, SuitMarker>.PooledList pooledList = ListPool<SuitLocker, SuitMarker>.Allocate();
            suitMarker.GetAttachedLockers(pooledList);
            for( int num = 0; num < pooledList.Count; ++num )
            {
                if( SuitLockerAutoDelivery.IsApplicableLocker( pooledList[ num ], suitMarker ))
                    return false; // disable the notification
            }
            pooledList.Recycle();
            return true;
        }
    }

    public class FastTrack_SuitMarkerUpdater_Patch
    {
        private static readonly Type FastTrack_SuitMarkerUpdater_type = AccessTools.TypeByName(
            "PeterHan.FastTrack.GamePatches.SuitMarkerUpdater" );

        public static void Patch( Harmony harmony )
        {
            MethodInfo info = AccessTools.Method( FastTrack_SuitMarkerUpdater_type, "DropSuit" );
            if( info != null )
            {
                harmony.Patch( info, transpiler: new HarmonyMethod(
                    typeof( FastTrack_SuitMarkerUpdater_Patch ).GetMethod( nameof( FastTrackDropSuit ))));
                info = AccessTools.Method( FastTrack_SuitMarkerUpdater_type, "UnequipReact" );
                if( info != null )
                {
                    harmony.Patch( info, prefix: new HarmonyMethod(
                        typeof( FastTrack_SuitMarkerUpdater_Patch ).GetMethod( nameof( FastTrackDropSuit_Prefix ))));
                    harmony.Patch( info, postfix: new HarmonyMethod(
                        typeof( FastTrack_SuitMarkerUpdater_Patch ).GetMethod( nameof( FastTrackDropSuit_Postfix ))));
                }
                else
                {
                    Debug.LogWarning("AutoSuitDelivery: Failed to patch FastTrack SuitMarkerUpdater.UnequipReact()");
                }
            }
        }

        public static IEnumerable<CodeInstruction> FastTrackDropSuit(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found = false;
            for( int i = 0; i < codes.Count; ++i )
            {
//                Debug.Log("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                // The function has code:
                // if (assignable.TryGetComponent(out Notifier notifier))
                // Change to:
                // if (assignable.TryGetComponent(out Notifier notifier) && DropSuit_Hook())
                if( codes[ i ].opcode == OpCodes.Ldloca_S
                    && i + 2 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Callvirt
                    && codes[ i + 1 ].operand.ToString() == "Boolean TryGetComponent[Notifier](Notifier ByRef)"
                    && codes[ i + 2 ].opcode == OpCodes.Brfalse_S )
                {
                    codes.Insert( i + 3, new CodeInstruction( OpCodes.Call,
                        typeof( FastTrack_SuitMarkerUpdater_Patch ).GetMethod( nameof( DropSuit_Hook ))));
                    codes.Insert( i + 4, codes[ i + 2 ].Clone()); // copy the Brfalse_S
                    found = true;
                    break;
                }
            }
            if(!found)
                Debug.LogWarning("AutoSuitDelivery: Failed to patch FastTrack SuitMarkerUpdater.DropSuit()");
            return codes;
        }

        // FastTrack's DropSuit() doesn't provide the SuitMarker, so get it from the function that calls it.
        private static SuitMarker FastTrack_SuitMarker;

        public static void FastTrackDropSuit_Prefix(SuitMarker checkpoint)
        {
            FastTrack_SuitMarker = checkpoint;
        }

        public static void FastTrackDropSuit_Postfix()
        {
            FastTrack_SuitMarker = null;
        }

        public static bool DropSuit_Hook()
        {
            if( !Options.Instance.AvoidNotification )
                return true;
            return SuitMarker_UnequipSuitReactable_Patch.AllowNotification( FastTrack_SuitMarker );
        }
    }
}

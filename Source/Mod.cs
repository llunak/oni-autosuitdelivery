using HarmonyLib;
using System.Collections.Generic;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace AutoSuitDelivery
{
    public class Mod : KMod.UserMod2
    {
        public override void OnLoad( Harmony harmony )
        {
            base.OnLoad( harmony );
            PUtil.InitLibrary( false );
            new POptions().RegisterOptions( this, typeof( Options ));
            SuitMarker_UnequipSuitReactable_Patch.Patch( harmony );
        }

        public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<KMod.Mod> mods)
        {
            base.OnAllModsLoaded( harmony, mods );
            FastTrack_SuitMarkerUpdater_Patch.Patch( harmony );
        }
    }
}

using HarmonyLib;
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
        }
    }
}

using PeterHan.PLib.Options;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AutoSuitDelivery
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("https://github.com/llunak/oni-autosuitdelivery")]
    [ConfigFile(SharedConfigLocation: true)]
    public sealed class Options : SingletonOptions< Options >, IOptions
    {
        [Option("Delivery After Time (s)", "Time in in-game seconds after which a suit is delivered.")]
        [JsonProperty]
        public int DeliveryAfterTime { get; set; } = (int)Constants.SECONDS_PER_CYCLE;

        public override string ToString()
        {
            return string.Format("AutoSuitDelivery.Options[deliveryaftertime={0}]",
                DeliveryAfterTime);
        }

        public void OnOptionsChanged()
        {
            // 'this' is the Options instance used by the options dialog, so set up
            // the actual instance used by the mod. MemberwiseClone() is enough to copy non-reference data.
            Instance = (Options) this.MemberwiseClone();
        }

        public IEnumerable<IOptionsEntry> CreateOptions()
        {
            return null;
        }
    }
}

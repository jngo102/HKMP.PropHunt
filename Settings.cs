using Modding.Converters;
using Newtonsoft.Json;
using PropHunt.Input;

namespace PropHunt
{
    public class GlobalSettings
    {
        [JsonConverter(typeof(PlayerActionSetConverter))]
        public PropActions Bindings { get; set; } = new();
    }
}
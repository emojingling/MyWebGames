using Newtonsoft.Json;

namespace DrawGuess.SignalR
{
    public class LineInfo
    {
        [JsonProperty("l1")]
        public double LeftFrom { get; set; }
        [JsonProperty("t1")]
        public double TopFrom { get; set; }
        [JsonProperty("l2")]
        public double LeftTo { get; set; }
        [JsonProperty("t2")]
        public double TopTo { get; set; }
        [JsonProperty("c")]
        public string Color { get; set; }
        [JsonProperty("w")]
        public string LineWidth { get; set; }
    }
}
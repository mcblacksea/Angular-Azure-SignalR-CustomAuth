namespace SignalRCustomAuthServer.Model {

    using System;
    using Newtonsoft.Json;

    public class TokenModel {

        [JsonProperty("aud")]
        public String Audience { get; set; }

        public DateTime Expires {
            get {
                return DateTimeOffset.FromUnixTimeSeconds(this.ExpiresSeconds).DateTime;
            }
        }

        [JsonProperty("exp")]
        public Int64 ExpiresSeconds { get; set; }

        [JsonProperty("userid")]
        public String UserId { get; set; }

        [JsonProperty("unique_name")]
        public String UserName { get; set; }

        public TokenModel() {
        }
    }
}

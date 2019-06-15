namespace SignalRCustomAuthServer.Model {

    using System;
    using System.Collections.Generic;
    using System.Security.Claims;

    public class TokenModel {
        public String Audience { get; set; }

        public DateTime Expires {
            get {
                return DateTimeOffset.FromUnixTimeSeconds(this.ExpiresSeconds).DateTime;
            }
        }

        public Int64 ExpiresSeconds { get; set; }
        public String Issuer { get; set; }
        public String UserId { get; set; }
        public String UserName { get; set; }

        public TokenModel() {
        }

        public static TokenModel CreateFromClaims(IEnumerable<Claim> claims) {
            var tokenModel = new TokenModel();
            foreach (var claim in claims) {
                switch (claim.Type) {
                    case "username":
                        tokenModel.UserName = claim.Value;
                        break;

                    case "userid":
                        tokenModel.UserId = claim.Value;
                        break;

                    case "exp":
                        tokenModel.ExpiresSeconds = Convert.ToInt64(claim.Value);
                        break;

                    case "aud":
                        tokenModel.Audience = claim.Value;
                        break;

                    case "iss":
                        tokenModel.Issuer = claim.Value;
                        break;

                    default:
                        break;
                }
            }
            return tokenModel;
        }
    }
}

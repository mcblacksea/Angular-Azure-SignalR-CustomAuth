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
                    case Global.ClaimUserName:
                        tokenModel.UserName = claim.Value;
                        break;

                    case Global.ClaimUserId:
                        tokenModel.UserId = claim.Value;
                        break;

                    case Global.ClaimExpires:
                        tokenModel.ExpiresSeconds = Convert.ToInt64(claim.Value);
                        break;

                    case Global.ClaimAudience:
                        tokenModel.Audience = claim.Value;
                        break;

                    case Global.ClaimIssuer:
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

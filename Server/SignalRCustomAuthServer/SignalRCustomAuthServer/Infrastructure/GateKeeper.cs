namespace SignalRCustomAuthServer.Infrastructure {

    using System;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Http;
    using SignalRCustomAuthServer.Model;

    public static class GateKeeper {

        public static ValidateTokenResult ValidateToken(IHeaderDictionary headers) {
            if (headers.ContainsKey(Global.AuthorizationHeaderName)) {
                if (headers[Global.AuthorizationHeaderName].ToString().StartsWith(Global.BearerPrefix)) {
                    var token = headers[Global.AuthorizationHeaderName].ToString().Substring(Global.BearerPrefix.Length);
                    var jwtTools = new JwtTools();
                    ClaimsPrincipal claimsPrincipal;

                    try {
                        claimsPrincipal = jwtTools.ValidateToken(Global.Issuer, Global.Audience, Global.EncryptionKey, token);
                    } catch (Exception ex) {
                        return new ValidateTokenResult(ex);
                    }

                    var tokenModel = jwtTools.ParseToken(token);
                    return new ValidateTokenResult(tokenModel);
                }
                return new ValidateTokenResult(new InvalidOperationException("Bearer missing"));
            }
            return new ValidateTokenResult(new InvalidOperationException("Authorization header missing."));
        }
    }
}

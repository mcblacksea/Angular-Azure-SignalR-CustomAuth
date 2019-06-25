namespace SignalRCustomAuthServer.Infrastructure {

    using System;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Http;
    using SignalRCustomAuthServer.Model;

    public static class GateKeeper {

        /// <summary>
        /// <para>Validates the token and returns the token properites in a <see cref="TokenModel"/>, any exceptions are included in the <see cref="ValidateTokenResult"/></para>
        /// <para>The reason for wrapping the results of this method in a class is to make it easier for the caller to deal with the many possible exceptions that can be thrown when validating a token.</para>
        /// </summary>
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

                    var tokenModel = TokenModel.CreateFromClaims(claimsPrincipal.Claims);
                    return new ValidateTokenResult(tokenModel);
                }
                return new ValidateTokenResult(new InvalidOperationException(Global.BearerMissing));
            }
            return new ValidateTokenResult(new InvalidOperationException(Global.AuthorizationHeaderMissing));
        }
    }
}

namespace SignalRCustomAuthServer.Infrastructure {

    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Microsoft.IdentityModel.Tokens;

    public class JwtTools {

        public JwtTools() {
        }

        public String CreateToken(String issuer, String audience, ClaimsIdentity subject, DateTime utcExpires, String signingKey) {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var token = jwtTokenHandler.CreateJwtSecurityToken(
                issuer: issuer,
                audience: audience,
                subject: subject,
                expires: utcExpires,
                signingCredentials: credentials);
            return jwtTokenHandler.WriteToken(token);
        }

        /// <summary>
        /// <para>WARNING - all callers of this method MUST be wrapped in a try / catch block!</para>
        /// <para>Interally this method invokes the JwtSecurityTokenHandler.ValidateToken which throws a different exception for any validatation failures, making it very easy to understand why the validation failed.</para>
        /// <para>See https://docs.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens.jwt.jwtsecuritytokenhandler.validatetoken?view=azure-dotnet for complete listing of thown exceptions.</para>
        /// </summary>
        public ClaimsPrincipal ValidateToken(String issuer, String audience, String signingKey, String accessToken) {
            var validationParameters =
                new TokenValidationParameters {
                    ValidAudience = audience,
                    ValidateAudience = true,
                    ValidIssuer = issuer,
                    ValidateIssuer = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    LifetimeValidator = (_, expires, __, ___) =>
                        expires.HasValue && expires > DateTime.UtcNow.AddMinutes(5) // at least 5 minutes
                };
            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(accessToken, validationParameters, out _);
        }
    }
}

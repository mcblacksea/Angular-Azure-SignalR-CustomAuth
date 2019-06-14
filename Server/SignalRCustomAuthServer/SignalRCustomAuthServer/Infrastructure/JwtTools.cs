namespace SignalRCustomAuthServer.Infrastructure {

    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.IdentityModel.Tokens;
    using Newtonsoft.Json;
    using SignalRCustomAuthServer.Model;

    public class JwtTools {

        public JwtTools() {
        }

        String FromBase64ToString(String part) {
            var bytes = WebEncoders.Base64UrlDecode(part);
            return Encoding.UTF8.GetString(bytes);
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

        public TokenModel ParseToken(String token) {
            var parts = token.Split(".");
            //var headerJson = FromBase64ToString(parts[0]);
            var payloadJson = FromBase64ToString(parts[1]);
            //var signatureJson = FromBase64ToString(parts[2]);

            var tokenModel = JsonConvert.DeserializeObject<TokenModel>(payloadJson);

            return tokenModel;
        }

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

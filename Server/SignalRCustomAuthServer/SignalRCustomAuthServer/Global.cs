namespace SignalRCustomAuthServer {

    using System;

    public class Global {
        public const String Audience = "https://oceanware.wordpress.com";
        public const String AuthorizationHeaderMissing = "Authorization header missing.";
        public const String AuthorizationHeaderName = "Authorization";
        public const String AzureWebJobsStorage = "AzureWebJobsStorage";
        public const String BearerMissing = "Bearer missing";
        public const String BearerPrefix = "Bearer ";
        public const String ClaimAudience = "aud";
        public const String ClaimExpires = "exp";
        public const String ClaimIssuer = "iss";
        public const String ClaimUserId = "userid";
        public const String ClaimUserName = "username";
        public const String EncryptionKey = "PleaseGetYourSigningKeyFromTheKeyVaultAtRunTime";
        public const String HttpVerbDelete = "delete";
        public const String HttpVerbGet = "get";
        public const String HttpVerbPatch = "patch";
        public const String HttpVerbPost = "post";
        public const String Issuer = "Oceanware";
        public const String MessageTarget = "notify";
        public const String PartitionKey = "PartitionKey";
        public const String SignalRHubName = "broadcast";
        public const String UserIdQueryParam = "/{userId}";
        public const String UserName = "UserName";
        public const String UserPartitionKey = "AzureRocks";
        public const String UserTableName = "Users";

    }
}

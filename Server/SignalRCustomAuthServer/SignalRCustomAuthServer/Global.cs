namespace SignalRCustomAuthServer {

    using System;

    public class Global {
        public const String Audience = "https://oceanware.wordpress.com";
        public const String AuthorizationHeaderName = "Authorization";
        public const String BearerPrefix = "Bearer ";
        public const String EncryptionKey = "PleaseGetYourSigningKeyFromTheKeyVaultAtRunTime";
        public const String Issuer = "Oceanware";
        public const String MessageTarget = "notify";
        public const String SignalRHubName = "broadcast";
        public const String UserPartitionKey = "AzureRocks";
        public const String UserTableName = "Users";
    }
}

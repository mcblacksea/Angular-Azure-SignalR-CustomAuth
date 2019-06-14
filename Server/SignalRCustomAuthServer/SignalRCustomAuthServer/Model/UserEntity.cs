namespace SignalRCustomAuthServer.Model {

    using System;
    using Microsoft.WindowsAzure.Storage.Table;

    public class UserEntity : TableEntity {
        public String Id { get; set; }
        public String PasswordHash { get; set; }
        public String UserName { get; set; }

        public UserEntity() {
        }
    }
}

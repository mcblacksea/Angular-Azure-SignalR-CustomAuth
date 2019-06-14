namespace SignalRCustomAuthServer.Model {

    using System;

    public class UserCreateModel {
        public String Id { get; set; }
        public String Password { get; set; }
        public String UserName { get; set; }

        public UserCreateModel() {
        }

        public Boolean IsValid() {
            if (String.IsNullOrWhiteSpace(this.Id)) {
                return false;
            }
            if (String.IsNullOrWhiteSpace(this.Password)) {
                return false;
            }
            if (String.IsNullOrWhiteSpace(this.UserName)) {
                return false;
            }

            return true;
        }
    }
}

namespace SignalRCustomAuthServer.Model {

    using System;

    public class UserLoginModel {
        public String Password { get; set; }
        public String UserName { get; set; }

        public UserLoginModel() {
        }

        public Boolean IsValid() {
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

namespace SignalRCustomAuthServer.Model {

    using System;

    public class ValidateTokenResult {
        public Exception Exception { get; }
        public TokenModel TokenModel { get; }
        public Boolean Unauthorized { get { return this.Exception != null; } }

        public ValidateTokenResult(TokenModel tokenModel) {
            this.TokenModel = tokenModel;
        }

        public ValidateTokenResult(Exception exception) {
            this.Exception = exception;
        }

        public String LogMessage() {
            if (this.Exception != null) {
                return String.Format(GlobalMessages.ExceptionThownWhileValidatingTokenFormat, this.Exception.GetType().Name, this.Exception.Message);
            }
            return String.Format(GlobalMessages.TokenForUserNameIsValidFormat, this.TokenModel.UserName);
        }
    }
}

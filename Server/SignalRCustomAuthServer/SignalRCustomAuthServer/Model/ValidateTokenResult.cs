namespace SignalRCustomAuthServer.Model {

    using System;

    public class ValidateTokenResult {
        public Exception Exception { get; }
        public TokenModel TokenModel { get; }

        public ValidateTokenResult(TokenModel tokenModel) {
            this.TokenModel = tokenModel;
        }

        public ValidateTokenResult(Exception exception) {
            this.Exception = exception;
        }

        public Boolean IsValid() {
            return this.TokenModel != null;
        }

        public String LogMessage() {
            if (this.Exception != null) {
                return $"{this.Exception.GetType().Name} was thrown while validating token.  Message: {this.Exception.Message}.";
            }
            return $"Token for user name: {this.TokenModel.UserName} is valid";
        }
    }
}

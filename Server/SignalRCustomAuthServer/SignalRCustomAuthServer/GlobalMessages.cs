namespace SignalRCustomAuthServer {

    using System;

    public class GlobalMessages {
        public const String ExceptionThownWhileValidatingTokenFormat = "{0} was thrown while validating token. Message: {1}.";
        public const String InvalidDataOneOrMoreValuesWasEmpty = "Invalid data, one or more values was empty.";
        public const String LoginFailedDueToInvalidData = "Login failed due to invalid data.";
        public const String LoginFailedFormat = "Login failed for {0} during login attempt.";
        public const String LoginFailedUserNotInDatabaseFormat = "Login attempt failed for {0}, user not in database.";
        public const String LoginSuccessfulFormat = "Login sucessful for {0}.";
        public const String SendingMessageOnlyToUserFormat = "Sending, {0} to {1} only.";
        public const String SendingMessageToAllUsersFormat = "Sending, {0} to all users.";
        public const String TokenForUserNameIsValidFormat = "Token for user name: {0} is valid";
        public const String UserAddedToDatabaseFormat = "User {0} added to database.";
        public const String UserAlreadyInDatabaseFormat = "User {0} already in database.";
    }
}

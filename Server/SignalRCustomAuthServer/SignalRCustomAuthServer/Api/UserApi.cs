namespace SignalRCustomAuthServer.Api {

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Azure.WebJobs.Extensions.SignalRService;
    using Microsoft.Extensions.Logging;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using SignalRCustomAuthServer.Extensions;
    using SignalRCustomAuthServer.Infrastructure;
    using SignalRCustomAuthServer.Model;

    public static class UserApi {

        static async Task<UserEntity> GetUserEntityByUserName(CloudTable userCloudTable, String userName) {
            var partitionKeyfilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Global.UserPartitionKey);
            var userNameFilter = TableQuery.GenerateFilterCondition("UserName", QueryComparisons.Equal, userName);
            var combinedFilter = TableQuery.CombineFilters(partitionKeyfilter, TableOperators.And, userNameFilter);
            var tableQuery = new TableQuery<UserEntity>().Where(combinedFilter);

            TableContinuationToken token = null;
            var results = await userCloudTable.ExecuteQuerySegmentedAsync<UserEntity>(tableQuery, token);
            if (results.Results.Count == 1) {
                return results.Results[0];
            }
            return null;
        }

        static async Task<UserEntity> GeUserEntityById(CloudTable userCloudTable, String userId) {
            var findOperation = TableOperation.Retrieve<UserEntity>(Global.UserPartitionKey, userId);
            var findResult = await userCloudTable.ExecuteAsync(findOperation);
            if (findResult.Result == null) {
                return null;
            }
            return (UserEntity)findResult.Result;
        }

        static UserEntity MakeUserEntity(String userName, String password, String id) {
            var userCreateModel = new UserCreateModel { UserName = userName, Password = password, Id = id };
            return userCreateModel.ToUserEntity();
        }

        [FunctionName(nameof(Login))]
        public static async Task<IActionResult> Login(
         [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
         [Table(Global.UserTableName, Connection = "AzureWebJobsStorage")] CloudTable userCloudTable,
         ILogger log) {
            using (var sr = new StreamReader(req.Body)) {
                var requestBodyJson = await sr.ReadToEndAsync();
                var userLoginModel = JsonConvert.DeserializeObject<UserLoginModel>(requestBodyJson);
                if (!userLoginModel.IsValid()) {
                    return new BadRequestObjectResult("Invalid data, one or more values was empty.");
                }
                var userEntity = await GetUserEntityByUserName(userCloudTable, userLoginModel.UserName);
                if (userEntity == null) {
                    return new UnauthorizedResult();
                }

                if (userLoginModel.UserName == userEntity.UserName && PasswordHashing.VerifyHashedPassword(userEntity.PasswordHash, userLoginModel.Password)) {
                    var jwtTools = new JwtTools();

                    var subject = new ClaimsIdentity(new[] {
                            new Claim(ClaimTypes.Name, userEntity.UserName),
                            new Claim("userid", userEntity.RowKey)
                        });

                    var utcExpiresDateTime = DateTime.UtcNow.AddHours(4);

                    var token = jwtTools.CreateToken(Global.Issuer, Global.Audience, subject, utcExpiresDateTime, Global.EncryptionKey);

                    var tokenItemModel = new TokenItemModel { Token = token };

                    return new OkObjectResult(tokenItemModel);
                }

                log.LogWarning($"Login failed for {userLoginModel.UserName} during login attempt.");
                return new UnauthorizedResult();
            }
        }

        [FunctionName(nameof(SeedDatabase))]
        public static async Task<IActionResult> SeedDatabase(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Table(Global.UserTableName, Connection = "AzureWebJobsStorage")] CloudTable userCloudTable,
            ILogger log) {

            var userA = MakeUserEntity("John", "john", "56bc6a96-d6dc-406e-b36f-46373936c3bd");
            var userB = MakeUserEntity("Sue", "sue", "8f9b0fa5-4afe-4120-9e99-4f32148a79fc");
            var userC = MakeUserEntity("Tim", "tim", "197f8915-a37f-41d0-97da-5514d85966b5");

            var batchOperation = new TableBatchOperation();
            batchOperation.InsertOrReplace(userA);
            batchOperation.InsertOrReplace(userB);
            batchOperation.InsertOrReplace(userC);

            var results = await userCloudTable.ExecuteBatchAsync(batchOperation);
            var userItemModels = new List<UserItemModel>();

            foreach (var result in results) {
                if (result.Result is UserEntity userEntity) {
                    userItemModels.Add(userEntity.ToUserItemModel());
                    log.LogInformation($"User {userEntity.UserName} in database.");
                }
            }

            return new OkObjectResult(userItemModels);
        }

        [FunctionName(nameof(SendMessageToAllUsers))]
        public static async Task<IActionResult> SendMessageToAllUsers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req,
            [SignalR(HubName = Global.SignalRHubName)]IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log) {
            var gateKeeperResult = GateKeeper.ValidateToken(req.Headers);
            if (gateKeeperResult.IsValid()) {
                using (var sr = new StreamReader(req.Body)) {
                    var message = sr.ReadToEnd();

                    log.LogInformation($"Sending, {message} to all users.");

                    await signalRMessages.AddAsync(
                        new SignalRMessage {
                            Target = Global.MessageTarget,
                            Arguments = new[] { message }
                        });

                    return new OkResult();
                }
            } else {
                log.LogError(gateKeeperResult.Exception, gateKeeperResult.LogMessage());
                return new UnauthorizedResult();
            }
        }

        [FunctionName(nameof(SendMessageToUser))]
        public static async Task<IActionResult> SendMessageToUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = nameof(SendMessageToUser) + "/{userId}")] HttpRequest req,
            [SignalR(HubName = Global.SignalRHubName)]IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log,
            String userId
            ) {
            var gateKeeperResult = GateKeeper.ValidateToken(req.Headers);
            if (gateKeeperResult.IsValid()) {
                using (var sr = new StreamReader(req.Body)) {
                    var message = sr.ReadToEnd();

                    log.LogInformation($"Sending, {message} to {userId}.");

                    await signalRMessages.AddAsync(
                        new SignalRMessage {
                            UserId = userId,
                            Target = Global.MessageTarget,
                            Arguments = new[] { message }
                        });

                    return new OkResult();
                }
            } else {
                log.LogError(gateKeeperResult.Exception, gateKeeperResult.LogMessage());
                return new UnauthorizedResult();
            }
        }

        [FunctionName(nameof(SignalRConnection))]
        public static IActionResult SignalRConnection(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            IBinder binder,
            ILogger log) {
            var gateKeeperResult = GateKeeper.ValidateToken(req.Headers);
            if (gateKeeperResult.IsValid()) {
                var connectionInfo = binder.Bind<SignalRConnectionInfo>(new SignalRConnectionInfoAttribute { HubName = Global.SignalRHubName, UserId = gateKeeperResult.TokenModel.UserId });
                return new OkObjectResult(connectionInfo);
            } else {
                log.LogError(gateKeeperResult.Exception, gateKeeperResult.LogMessage());
                return new UnauthorizedResult();
            }
        }
    }
}

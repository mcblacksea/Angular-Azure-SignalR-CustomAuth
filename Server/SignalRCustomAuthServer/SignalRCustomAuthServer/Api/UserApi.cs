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
        
        // in a real world app using Table Storage with thousands of users, create a second table that maps a userName to the full user entity table.
        // or, use CosmosDB which supports multiple indexes.
        static async Task<UserEntity> GetUserEntityByUserName(CloudTable userCloudTable, String userName) {
            var partitionKeyfilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Global.UserPartitionKey);
            var userNameFilter = TableQuery.GenerateFilterCondition("UserName", QueryComparisons.Equal, userName);
            var combinedFilter = TableQuery.CombineFilters(partitionKeyfilter, TableOperators.And, userNameFilter);
            var tableQuery = new TableQuery<UserEntity>().Where(combinedFilter);

            TableContinuationToken continuationToken = null;
            var results = await userCloudTable.ExecuteQuerySegmentedAsync<UserEntity>(tableQuery, continuationToken);
            if (results.Results.Count == 1) {
                return results.Results[0];
            }
            return null;
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
                var requestBodyJson = sr.ReadToEnd();
                var userLoginModel = JsonConvert.DeserializeObject<UserLoginModel>(requestBodyJson);
                if (!userLoginModel.IsValid()) {
                    log.LogWarning($"Login failed due to invalid data.");
                    return new BadRequestObjectResult("Invalid data, one or more values was empty.");
                }
                var userEntity = await GetUserEntityByUserName(userCloudTable, userLoginModel.UserName);
                if (userEntity == null) {
                    log.LogWarning($"Login attempt failed for {userLoginModel.UserName}, user not in database.");
                    return new UnauthorizedResult();
                }

                if (userLoginModel.UserName.Equals(userEntity.UserName, StringComparison.OrdinalIgnoreCase) 
                    && PasswordHashing.VerifyHashedPassword(userEntity.PasswordHash, userLoginModel.Password)) {

                    var jwtTools = new JwtTools();

                    var subject = new ClaimsIdentity(new[] {
                            new Claim(ClaimTypes.Name, userEntity.UserName),
                            new Claim("userid", userEntity.RowKey)
                        });

                    var utcExpiresDateTime = DateTime.UtcNow.AddHours(4);  // you can change this to meet your requirements for token expiration.

                    var token = jwtTools.CreateToken(Global.Issuer, Global.Audience, subject, utcExpiresDateTime, Global.EncryptionKey);

                    var tokenItemModel = new TokenItemModel { Token = token };

                    log.LogInformation($"Login sucessful for {userLoginModel.UserName}.");
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

            var userItemModels = new List<UserItemModel>();

            TableContinuationToken continuationToken = null;
            var entities = await userCloudTable.ExecuteQuerySegmentedAsync<UserEntity>(new TableQuery<UserEntity>(), continuationToken);
            if (entities.Results.Count > 0) {
                foreach (var userEntity in entities.Results) {
                    userItemModels.Add(userEntity.ToUserItemModel());
                    log.LogInformation($"User {userEntity.UserName} in database.");
                }
                return new OkObjectResult(userItemModels);
            }
            
            // database is empty - now seed it

            var userA = MakeUserEntity("John", "john", "56bc6a96-d6dc-406e-b36f-46373936c3bd");
            var userB = MakeUserEntity("Sue", "sue", "8f9b0fa5-4afe-4120-9e99-4f32148a79fc");
            var userC = MakeUserEntity("Tim", "tim", "197f8915-a37f-41d0-97da-5514d85966b5");

            var batchOperation = new TableBatchOperation();
            batchOperation.InsertOrReplace(userA);
            batchOperation.InsertOrReplace(userB);
            batchOperation.InsertOrReplace(userC);

            var results = await userCloudTable.ExecuteBatchAsync(batchOperation);
            
            foreach (var result in results) {
                if (result.Result is UserEntity userEntity) {
                    userItemModels.Add(userEntity.ToUserItemModel());
                    log.LogInformation($"User {userEntity.UserName} added to database.");
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

                    log.LogInformation($"Sending, {message} to {userId} only.");

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

        /// <summary>
        /// Utilizes an output binding to create the SignalRConnection by reading the request header Authorization JWT and adsigning the UserId in the JWT to the SignalRConnection UserId property.
        /// </summary>
        /// <remarks>
        /// <para>The below IBinder is an output binding that returns a SignalRConnectionInfo to the caller.</para>
        /// <para>The reason for this output binding is because the input binder that uses the SignalRConnectionInfoAttribute, requires that the UserId be set as seen below:</para>
        /// <para>Input Binding [SignalRConnectionInfo(HubName = SignalRHubName, UserId = "{headers.x-ms-client-principal-id}")] SignalRConnectionInfo connectionInfo,</para>
        /// <para>Notice how the UserId is set from an http header value.  This can be considered a security risk so we want to avoid this if possible.</para>
        /// <para>This output binding creates the SignalRConnectionInfo internally and setting the UserId to the UserId inside the Authentication JWT token.</para>
        /// <para>The two below posts and code examples along with Anthony Chu's guidance make this feature possible.</para>
        /// <para>https://gist.github.com/ErikAndreas/72c94a0c8a9e6e632f44522c41be8ee7 </para>
        /// <para>http://dontcodetired.com/blog/post/Dynamic-Binding-in-Azure-Functions-with-Imperative-Runtime-Bindings </para>
        /// <para>http://dontcodetired.com/blog/post/Creating-Custom-Azure-Functions-Bindings</para>
        /// <para>The GateKeeper validates the Authentication token. Yes there is some ceremony code for each method, but I prefer to be in control and the code is simple.</para>
        /// <para>Ben Morris has published an alturnative solution using an input binding to perform the validation, this solution has much less ceremony code. https://www.ben-morris.com/custom-token-authentication-in-azure-functions-using-bindings/</para>
        /// </remarks>
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

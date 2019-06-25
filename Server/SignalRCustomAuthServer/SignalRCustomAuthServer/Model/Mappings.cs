namespace SignalRCustomAuthServer.Model {

    using SignalRCustomAuthServer.Extensions;

    public static class Mappings {

        public static UserEntity ToUserEntity(this UserCreateModel usercreateModel) {
            return new UserEntity {
                PartitionKey = Global.UserPartitionKey,
                RowKey = usercreateModel.Id,
                Id = usercreateModel.Id,
                PasswordHash = PasswordHashing.HashPassword(usercreateModel.Password),
                UserName = usercreateModel.UserName
            };
        }

        public static UserItem ToUserItem(this UserEntity userEntity) {
            return new UserItem {
                Id = userEntity.Id,
                UserName = userEntity.UserName
            };
        }
    }
}

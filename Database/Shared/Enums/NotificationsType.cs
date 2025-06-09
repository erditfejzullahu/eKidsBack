namespace eKids.Shared.Enums
{
    public enum NotificationsType
    {
        Info = 1, //for all infos about acc and other
        Warning = 2,
        Error = 3,
        UserFriendReq = 4, //if userreq for example friend requests and stuff
        UserActionReq = 5,
        UserFriendAccepted = 6,

        LoginActivity = 10,
        PasswordReset = 11,
        RegisteredAccount = 12,
        FriendRequestSended = 13,
        FriendRequestReceived = 14,
        FriendRequestSenderAccepted = 15,
        FriendRequestReceiverAccepted = 16,
        CustomInformaionOrPromotionsSendToAll = 17,
    }
}

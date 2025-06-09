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
        FriendRequestSenderAccepted = 15, //personi qe ja dergon requestin, e merr informaten qe esht pranu nga personi
        FriendRequestReceiverAccepted = 16, //personi qe e pranon requestin, e merr informaten qe e ka pranu personin
        CustomInformaionOrPromotionsSendToAll = 17, //informacion cfaredo
        ProgressTrackingNotification = 18, //kur fillon dicka si quiz, kurs etjetj
        CompletedProgressNotification = 19 //kur mbaron diqka si quiz kurs etjetj
    }
}

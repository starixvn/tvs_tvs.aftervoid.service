namespace Fanex.TVS.AfterVoid.Core.SendEmail.Models
{
    public enum EmailGroup
    {
        None,
        Csd,
        ItQa,
        ItSupport,
        CashOut,
        Recover
    }

    public static class ExceptionGroup
    {
        public const string WrongGroup = "Exception Subject";
    }

    public static class ErrorSendEmail
    {
        public const string Csd = "Error when send email to CSD group";
        public const string Recover = "Error when send email to Recover Bet group";
        public const string CashOut = "Error when send email to Cash Out group";
        public const string It = "Error when send email to IT group";
        public const string WrongGroup = "Error when send email to wrong group";
    }
}
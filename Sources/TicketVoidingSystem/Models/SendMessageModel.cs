namespace TicketVoidingSystem.Models
{
    public interface SendMessageModel
    {
        int UserId { get; }

        string UserName { get; }

        int AgentId { get; }

        string AgentName { get; }

        int MasterId { get; }

        string MasterName { get; }

        int SuperId { get; }

        string SuperName { get; }

        long TransId { get; }
    }

    public interface SendMessageDbModel
    {
        int CustId { get; }

        string UserName { get; }

        int Recommend { get; }

        string AgUserName { get; }

        int Mrecommend { get; }

        string MagUserName { get; }

        int Srecommend { get; }

        string SagUserName { get; }

        long TransId { get; }
    }
}
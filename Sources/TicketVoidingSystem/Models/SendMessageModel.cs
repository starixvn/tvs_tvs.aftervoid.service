namespace TicketVoidingSystem.Models;

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
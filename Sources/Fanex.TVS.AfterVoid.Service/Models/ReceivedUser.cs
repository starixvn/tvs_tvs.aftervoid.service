namespace Fanex.TVS.AfterVoid.Service.Models;

public class ReceivedUser
{
    public long TransId { get; set; }

    public int CustId { get; set; }

    public string CustUserName { get; set; }

    public int Recommend { get; set; }

    public string AgUserName { get; set; }

    public int Mrecommend { get; set; }

    public string MagUserName { get; set; }

    public int Srecommend { get; set; }

    public string SagUserName { get; set; }
}
namespace Fanex.TVS.AfterVoid.Core.SendEmail.Services.Subject
{
    using Resources;

    public interface SubjectLabelsService
    {
        string It();

        string Licensee();
    }

    public class SubjectVoidImp : SubjectLabelsService
    {
        public string Licensee() => Labels.Subject_VoidedBetsReportFor;

        public string It() => Labels.Subject_VoidedBetsReportForIt;
    }

    public class SubjectVoidAbnormalImp : SubjectLabelsService
    {
        public string Licensee() => Labels.Subject_VoidedAbnormalBetsReport;

        public string It() => Labels.Subject_VoidedAbnormalBetsReportIt;
    }

    public class SubjectVoidCashOutImp : SubjectLabelsService
    {
        public string Licensee() => Labels.Subject_VoidedCashOut;

        public string It() => Labels.Subject_VoidedCashOutIt;
    }

    public class SubjectVoidNumberGameImp : SubjectLabelsService
    {
        public string Licensee() => Labels.Subject_VoidedNumberGame;

        public string It() => Labels.Subject_VoidedNumberGameIt;
    }

    public class SubjectVoidVirtualSportsImp : SubjectLabelsService
    {
        public string It() => Labels.Subject_VoidedVirtualSportsIt;

        public string Licensee() => Labels.Subject_VoidedVirtualSports;
    }

    public class SubjectRecoverImp : SubjectLabelsService
    {
        public string Licensee() => Labels.Subject_RecoveredBetReport;

        public string It() => Labels.Subject_RecoveredBetReportIt;
    }

    public class SubjectRecoverCashOutImp : SubjectLabelsService
    {
        public string Licensee() => Labels.Subject_RecoveredCashOut;

        public string It() => Labels.Subject_RecoveredCashOutIt;
    }

    public class SubjectRecoverNumberGameImp : SubjectLabelsService
    {
        public string Licensee() => Labels.Subject_RecoveredNumberGame;

        public string It() => Labels.Subject_RecoveredNumberGameIt;
    }

    public class SubjectRecoverParlayImp : SubjectLabelsService
    {
        public string Licensee() => Labels.Subject_RecoveredParlay;

        public string It() => Labels.Subject_RecoveredParlayIt;
    }

    public class SubjectRecoverVirtualSportsImp : SubjectLabelsService
    {
        public string Licensee() => Labels.Subject_RecoveredVirtualSports;

        public string It() => Labels.Subject_RecoveredVirtualSportsIt;
    }
}
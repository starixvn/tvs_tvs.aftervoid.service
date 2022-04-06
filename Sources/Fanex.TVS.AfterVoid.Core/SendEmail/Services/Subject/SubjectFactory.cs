namespace Fanex.TVS.AfterVoid.Core.SendEmail.Services.Subject
{
    public enum VoidType
    {
        None,
        Abnormal,
        CashOut,
        NumberGame,
        VirtualSports
    }

    public enum RecoverType
    {
        None,
        Default,
        CashOut,
        NumberGame,
        Parlay,
        VirtualSports
    }

    public static class SubjectFactory
    {
        public static SubjectLabelsService CreateVoid(VoidType type)
        {
            SubjectLabelsService subjectLabels;
            switch (type)
            {
                case VoidType.CashOut:
                    subjectLabels = new SubjectVoidCashOutImp();
                    break;

                case VoidType.NumberGame:
                    subjectLabels = new SubjectVoidNumberGameImp();
                    break;

                case VoidType.VirtualSports:
                    subjectLabels = new SubjectVoidVirtualSportsImp();
                    break;

                case VoidType.Abnormal:
                    subjectLabels = new SubjectVoidAbnormalImp();
                    break;

                default:
                    subjectLabels = new SubjectVoidImp();
                    break;
            }

            return subjectLabels;
        }

        public static SubjectLabelsService CreateRecover(RecoverType type)
        {
            SubjectLabelsService subjectLabels;
            switch (type)
            {
                case RecoverType.CashOut:
                    subjectLabels = new SubjectRecoverCashOutImp();
                    break;

                case RecoverType.NumberGame:
                    subjectLabels = new SubjectRecoverNumberGameImp();
                    break;

                case RecoverType.Parlay:
                    subjectLabels = new SubjectRecoverParlayImp();
                    break;

                case RecoverType.VirtualSports:
                    subjectLabels = new SubjectRecoverVirtualSportsImp();
                    break;

                default:
                    subjectLabels = new SubjectRecoverImp();
                    break;
            }

            return subjectLabels;
        }
    }
}
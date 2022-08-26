namespace Fanex.TVS.AfterVoid.Service.Constants
{
    public static class ExecuteStatus
    {
        public const byte New = 0;
        public const byte DBJobRunning = 1;
        public const byte DBJobDone = 2;
        public const byte DBJobFailed = 3;

        public const byte WinServiceDone = 12;
        public const byte WinServiceFailed = 13;
    }
}
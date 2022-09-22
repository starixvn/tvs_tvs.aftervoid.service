namespace TicketVoidingSystem.Constants
{
    public static class ExecuteStatus
    {
        public const byte New = 0;
        public const byte Running = 1;
        public const byte Done = 2;
        public const byte Failed = 3;

        public const byte QueueMixParlay = 100;
        public const byte VoidServiceExecuteVoidDone = 12;
    }
}
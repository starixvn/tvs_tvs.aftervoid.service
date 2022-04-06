namespace Fanex.TVS.AfterVoid.Core.Extensions
{
    public static class BetListExtension
    {
        private const int WallType = 2;

        public static byte SetWalletType(this byte walletType, long? refCode)
        {
            if (walletType == WallType && (refCode == 0 || refCode == null))
            {
                return 0;
            }

            return walletType;
        }
    }
}
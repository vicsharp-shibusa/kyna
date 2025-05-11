namespace Kyna.Research;

internal static class Constants
{
    internal static class Accounting
    {
        internal static class FundingRuleFrequencies
        {
            internal const string Once = "Once";
            internal const string EveryMonth = "Every Month";
            internal const string EveryWeek = "Every Week";
        }

        internal static class AccountNames
        {
            public static readonly string[] DefaultAccountNames = [];

            static AccountNames()
            {
                List<string> accountNames = [];
                foreach (var property in typeof(AccountNames)
                    .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                {
                    var obj = property.GetValue(null);
                    accountNames.Add(obj?.ToString() ?? "");
                }
                DefaultAccountNames = [.. accountNames];
            }

            public const string Cash = "CASH";
            public const string OwnersEquity = "OWNERS EQUITY";
            public const string InterestIncome = "INTEREST INCOME";
            public const string DividendIncome = "DIVIDEND INCOME";

            internal static string[] DepositAndWithdrawalAccounts => [Cash, OwnersEquity];
            public static bool IsDefaultAccount(string account) =>
                DefaultAccountNames.Contains(account, StringComparer.OrdinalIgnoreCase);
        }
    }
}

public enum LedgerEntryType
{
    Debit,
    Credit
}

public enum InstrumentTransactionType
{
    None = 0,
    Buy,
    Sell,
    InterestIncome,
    DividendIncome
}

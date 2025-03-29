namespace ZOUZ.Wallet.Core.Exceptions;

public class InsufficientBalanceException : BusinessRuleException
{
    public InsufficientBalanceException(string message) : base(message) { }
}
namespace ZOUZ.Wallet.Core.Exceptions;

public class OfferLimitExceededException : BusinessRuleException
{
    public OfferLimitExceededException(string message) : base(message) { }
}
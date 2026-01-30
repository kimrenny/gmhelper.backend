namespace MatHelper.CORE.Exceptions
{
    public class TwoFactorRequiredException : Exception
    {
        public TwoFactorRequiredException()
            : base("Two-factor authentication is required to perform this action.")
        {}
    }
}

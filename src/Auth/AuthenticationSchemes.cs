namespace Kosync.Auth;

public static class AuthenticationSchemes
{
    public static class KoReaderScheme
    {
        public const string KoReader = nameof(KoReader);
        public const string AuthHeaderName = "x-auth-user";
        public const string PasswordHeaderName = "x-auth-key";
    }
}

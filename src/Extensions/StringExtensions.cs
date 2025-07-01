using System.Security.Cryptography;
using System.Text;

namespace Kosync.Extensions;

public static class StringExtensions
{
    public static string HashPassword(this string password)
    {
        MD5 md5 = MD5.Create();

        md5.ComputeHash(Encoding.ASCII.GetBytes(password));

        byte[] result = md5.Hash!;

        StringBuilder strBuilder = new();
        foreach (byte t in result)
        {
            strBuilder.Append(t.ToString("x2"));
        }

        return strBuilder.ToString();
    }
}
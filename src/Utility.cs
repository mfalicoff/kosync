using System.Security.Cryptography;
using System.Text;

namespace Kosync;

public static class Utility
{
    public static string HashPassword(string password)
    {
        MD5 md5 = MD5.Create();

        md5.ComputeHash(Encoding.ASCII.GetBytes(password));

        byte[] result = md5.Hash!;

        StringBuilder strBuilder = new();
        for (int i = 0; i < result.Length; i++)
        {
            strBuilder.Append(result[i].ToString("x2"));
        }

        return strBuilder.ToString();
    }
}

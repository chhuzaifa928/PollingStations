using System;

namespace NID.Areas.ElectionTransport.Infrastructure
{
    internal static class TransportSecurity
    {
        public static bool FixedTimeEquals(string expected, string supplied)
        {
            if (expected == null || supplied == null)
            {
                return false;
            }

            int difference = expected.Length ^ supplied.Length;
            int length = Math.Max(expected.Length, supplied.Length);

            for (int i = 0; i < length; i++)
            {
                char left = i < expected.Length ? expected[i] : (char)0;
                char right = i < supplied.Length ? supplied[i] : (char)0;
                difference |= left ^ right;
            }

            return difference == 0;
        }
    }
}

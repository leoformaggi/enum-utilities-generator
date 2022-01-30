namespace EnumUtilitiesGenerator
{
    internal static class StringExtensions
    {
        public static string Quote(this string s)
        {
            return $"\"{s}\"";
        }
    }
}

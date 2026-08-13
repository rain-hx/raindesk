namespace rainspace.Util
{
    public class StringUtil
    {
        /// <summary>
        /// PERF: Call ToString() only once and use string.IsNullOrWhiteSpace.
        /// The old code called ToString() up to 3 times (once for null check, once for Length,
        /// once for Trim().Length), each allocating a new string for non-string objects.
        /// </summary>
        public static bool IsEmpty(object str)
        {
            if (str == null) return true;
            string s = str.ToString();
            return string.IsNullOrWhiteSpace(s);
        }
    }
}

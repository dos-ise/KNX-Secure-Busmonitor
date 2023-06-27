namespace KNX_Secure_Busmonitor_MAUI.Model
{
  public static class StringExtensionMethods
  {
    public static IEnumerable<string> GetLines(this string str, bool removeEmptyLines = false)
    {
      return str.Split(new[] { "\r\n", "\r", "\n" },
        removeEmptyLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
    }

    public static IEnumerable<string> GetColumns(this string str)
    {
      return str.Split(new[] { "\t" }, StringSplitOptions.None);
    }

    public static T[] Slice<T>(this T[] source, int start, int end)
    {
      // Handles negative ends.
      if (end < 0)
      {
        end = source.Length + end;
      }
      int len = end - start;

      // Return new array.
      T[] res = new T[len];
      for (int i = 0; i < len; i++)
      {
        res[i] = source[i + start];
      }
      return res;
    }
  }
}

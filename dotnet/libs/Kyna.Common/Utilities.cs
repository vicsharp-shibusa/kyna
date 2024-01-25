using System.ComponentModel;
using System.Reflection;

namespace Kyna.Common;

public static class EnumUtilities
{
    public static IEnumerable<string> GetDescriptions<T>() where T : struct, Enum
    {
        MemberInfo[] members = typeof(T).GetMembers(BindingFlags.Public | BindingFlags.Static);
        foreach (MemberInfo member in members)
        {
            var attrs = member.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attrs.Length > 0)
            {
                for (int i = 0; i < attrs.Length; i++)
                {
                    string description = ((DescriptionAttribute)attrs[i]).Description;

                    yield return description;
                }
            }
            else
            {
                yield return member.Name;
            }
        }
    }
}

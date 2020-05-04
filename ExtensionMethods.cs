using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace InactivityBot
{
    public static class ExtensionMethods
    {
        public static string GetEnumDescription(this Enum value)
        {
            FieldInfo fieldInfo = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] descriptions = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            if (descriptions != null && descriptions.Any())
            {
                return descriptions.First().Description;
            }

            return value.ToString();
        }
    }
}

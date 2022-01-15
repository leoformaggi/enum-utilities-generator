using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Benchmark
{
    public static class EnumUtils
    {
        public static string GetDescriptionFromEnum(this Enum value)
        {
            DescriptionAttribute[] array = (DescriptionAttribute[])value.GetType().GetField(value.ToString())!.GetCustomAttributes(typeof(DescriptionAttribute), inherit: false);
            if (array != null && array.Length != 0)
            {
                return array[0].Description;
            }

            return string.Empty;
        }

        public static T GetEnumFromDescription<T>(string description)
        {
            return GetEnumFromDescription<T>(description, notFoundReturnDefault: true);
        }

        public static T GetEnumFromDescription<T>(string description, bool notFoundReturnDefault)
        {
            Type typeFromHandle = typeof(T);
            if (!typeFromHandle.IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type.");
            }

            FieldInfo[] fields = typeFromHandle.GetFields();
            foreach (FieldInfo fieldInfo in fields)
            {
                DescriptionAttribute descriptionAttribute = Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (descriptionAttribute != null && descriptionAttribute.Description == description)
                {
                    return (T)fieldInfo.GetValue(null);
                }
            }

            if (notFoundReturnDefault)
            {
                return default(T);
            }

            throw new KeyNotFoundException(description + " not found in " + typeof(T).Name);
        }
    }
}

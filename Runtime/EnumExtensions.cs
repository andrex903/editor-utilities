using System;
using System.Collections.Generic;

public static class EnumExtensions
{
    #region Flags

    public static bool Has<T>(this Enum type, T value) where T : Enum
    {
        try
        {
            return ((type.ToInt() & value.ToInt()) == value.ToInt());
        }
        catch
        {
            return false;
        }
    }

    public static bool NotContains<T>(this Enum type, T value) where T : Enum
    {
        try
        {
            return ((type.ToInt() & value.ToInt()) == 0);
        }
        catch
        {
            return false;
        }
    }

    public static T Add<T>(this Enum type, T value) where T : Enum
    {
        return (T)(object)((type.ToInt() | value.ToInt()));
    }

    public static T Remove<T>(this Enum type, T value) where T : Enum
    {
        return (T)(object)((type.ToInt() & ~value.ToInt()));
    }

    #endregion

    #region Generic

    public static int ToInt(this Enum enumValue)
    {
        return Convert.ToInt32(enumValue);
    }

    static public bool TryToEnum<T>(this int value, out T result) where T : Enum
    {
        if (Enum.IsDefined(typeof(T), value))
        {
            result = (T)Enum.ToObject(typeof(T), value);
            return true;
        }
        else
        {
            result = default;
            return false;
        }
    }

    static public T ToEnum<T>(this int value) where T : Enum
    {
        value.TryToEnum(out T result);
        return result;
    }

    /// <summary>
    /// Returns the lenght of an enums
    /// </summary>  
    public static int Count(this Enum enumVal)
    {
        return Count(enumVal.GetType());
    }

    public static int Count(Type type)
    {
        return Enum.GetNames(type).Length;
    }

    public static IEnumerable<T> GetFlags<T>(this T flags) where T : Enum
    {
        foreach (Enum value in Enum.GetValues(flags.GetType()))
        {
            if (flags.HasFlag(value)) yield return (T)value;
        }
    }

    #endregion
}
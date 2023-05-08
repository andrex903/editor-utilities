using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class EnumFlagsAttribute : PropertyAttribute
{
    public EnumFlagsAttribute() { }
}

/// <summary>
/// Display an enum flag as a normal enum.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class EnumSingleAttribute : PropertyAttribute
{
    public Type Type { get; private set; }

    public EnumSingleAttribute(Type type)
    {
        this.Type = type;
    }
}
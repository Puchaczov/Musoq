using System;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts boolean value to bytes.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <returns>Bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[] ToBytes(bool value)
    {
        return BitConverter.GetBytes(value);
    }

    /// <summary>
    ///     Converts short value to bytes.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <returns>Bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[] ToBytes(short value)
    {
        return BitConverter.GetBytes(value);
    }

    /// <summary>
    ///     Converts ushort value to bytes.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <returns>Bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[] ToBytes(ushort value)
    {
        return BitConverter.GetBytes(value);
    }

    /// <summary>
    ///     Converts int value to bytes.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <returns>Bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[] ToBytes(int value)
    {
        return BitConverter.GetBytes(value);
    }


    /// <summary>
    ///     Converts uint value to bytes.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <returns>Bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[] ToBytes(uint value)
    {
        return BitConverter.GetBytes(value);
    }

    /// <summary>
    ///     Converts long value to bytes.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <returns>Bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[] ToBytes(long value)
    {
        return BitConverter.GetBytes(value);
    }

    /// <summary>
    ///     Converts long value to bytes.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <returns>Bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[] ToBytes(ulong value)
    {
        return BitConverter.GetBytes(value);
    }
}

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
    public byte[] ToBytes(bool value) => BitConverter.GetBytes(value);

    /// <summary>Gets the bytes of a nullable Boolean value.</summary>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[]? ToBytes(bool? value) => GetBytesNullable(value, BitConverter.GetBytes);

    /// <summary>
    ///     Converts short value to bytes.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <returns>Bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[] ToBytes(short value) => BitConverter.GetBytes(value);

    /// <summary>Gets the bytes of a nullable Int16 value.</summary>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[]? ToBytes(short? value) => GetBytesNullable(value, BitConverter.GetBytes);

    /// <summary>
    ///     Converts ushort value to bytes.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <returns>Bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[] ToBytes(ushort value) => BitConverter.GetBytes(value);

    /// <summary>Gets the bytes of a nullable UInt16 value.</summary>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[]? ToBytes(ushort? value) => GetBytesNullable(value, BitConverter.GetBytes);

    /// <summary>
    ///     Converts int value to bytes.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <returns>Bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[] ToBytes(int value) => BitConverter.GetBytes(value);

    /// <summary>Gets the bytes of a nullable Int32 value.</summary>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[]? ToBytes(int? value) => GetBytesNullable(value, BitConverter.GetBytes);

    /// <summary>
    ///     Converts uint value to bytes.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <returns>Bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[] ToBytes(uint value) => BitConverter.GetBytes(value);

    /// <summary>Gets the bytes of a nullable UInt32 value.</summary>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[]? ToBytes(uint? value) => GetBytesNullable(value, BitConverter.GetBytes);

    /// <summary>
    ///     Converts long value to bytes.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <returns>Bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[] ToBytes(long value) => BitConverter.GetBytes(value);

    /// <summary>Gets the bytes of a nullable Int64 value.</summary>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[]? ToBytes(long? value) => GetBytesNullable(value, BitConverter.GetBytes);

    /// <summary>
    ///     Converts ulong value to bytes.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <returns>Bytes.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[] ToBytes(ulong value) => BitConverter.GetBytes(value);

    /// <summary>Gets the bytes of a nullable UInt64 value.</summary>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public byte[]? ToBytes(ulong? value) => GetBytesNullable(value, BitConverter.GetBytes);
}

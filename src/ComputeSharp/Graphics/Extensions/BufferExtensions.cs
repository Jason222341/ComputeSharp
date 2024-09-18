using System;
using System.Runtime.InteropServices;
using ComputeSharp.Resources;

namespace ComputeSharp;

/// <summary>
/// A <see langword="class"/> that contains extension methods for the <see cref="Buffer{T}"/> type.
/// </summary>
public static class BufferExtensions
{
    /// <summary>
    /// Reads the contents of a <see cref="Buffer{T}"/> instance and returns an array.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffer.</typeparam>
    /// <param name="source">The input <see cref="Buffer{T}"/> instance to read data from.</param>
    /// <returns>A <typeparamref name="T"/> array with the contents of the input buffer.</returns>
    public static T[] ToArray<T>(this Buffer<T> source)
        where T : unmanaged
    {
        default(ArgumentNullException).ThrowIfNull(source);

        return source.ToArray(0, source.Length);
    }

    /// <summary>
    /// Reads the contents of a <see cref="Buffer{T}"/> instance in a given range and returns an array.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffer.</typeparam>
    /// <param name="source">The input <see cref="Buffer{T}"/> instance to read data from.</param>
    /// <param name="offset">The offset to start reading data from.</param>
    /// <param name="count">The number of items to read.</param>
    /// <returns>A <typeparamref name="T"/> array with the contents of the specified range from the current buffer.</returns>
    public static T[] ToArray<T>(this Buffer<T> source, int offset, int count)
        where T : unmanaged
    {
        default(ArgumentNullException).ThrowIfNull(source);
        default(ArgumentOutOfRangeException).ThrowIfNegative(count);

        T[] data = GC.AllocateUninitializedArray<T>(count);

        source.CopyTo(data.AsSpan(), offset);

        return data;
    }

    /// <summary>
    /// Reads the contents of a <see cref="Buffer{T}"/> instance and writes them into a target array.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffer.</typeparam>
    /// <param name="source">The input <see cref="Buffer{T}"/> instance to read data from.</param>
    /// <param name="destination">The input array to write data to.</param>
    public static void CopyTo<T>(this Buffer<T> source, T[] destination)
        where T : unmanaged
    {
        default(ArgumentNullException).ThrowIfNull(source);
        default(ArgumentNullException).ThrowIfNull(destination);

        source.CopyTo(destination.AsSpan(), 0);
    }

    /// <summary>
    /// Reads the contents of the specified range from a <see cref="Buffer{T}"/> instance and writes them into a target array.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffer.</typeparam>
    /// <param name="source">The input <see cref="Buffer{T}"/> instance to read data from.</param>
    /// <param name="destination">The input array to write data to.</param>
    /// <param name="sourceOffset">The offset to start reading data from.</param>
    /// <param name="destinationOffset">The starting offset within <paramref name="destination"/> to write data to.</param>
    /// <param name="count">The number of items to read.</param>
    public static void CopyTo<T>(this Buffer<T> source, T[] destination, int sourceOffset, int destinationOffset, int count)
        where T : unmanaged
    {
        default(ArgumentNullException).ThrowIfNull(source);
        default(ArgumentNullException).ThrowIfNull(destination);

        Span<T> span = destination.AsSpan(destinationOffset, count);

        source.CopyTo(span, sourceOffset);
    }

    /// <summary>
    /// Reads the contents of a <see cref="Buffer{T}"/> instance and writes them into a target <see cref="Span{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffer.</typeparam>
    /// <param name="source">The input <see cref="Buffer{T}"/> instance to read data from.</param>
    /// <param name="destination">The input <see cref="Span{T}"/> to write data to.</param>
    public static void CopyTo<T>(this Buffer<T> source, Span<T> destination)
        where T : unmanaged
    {
        default(ArgumentNullException).ThrowIfNull(source);

        source.CopyTo(destination, 0);
    }

    /// <summary>
    /// Reads the contents of the specified range from a <see cref="Buffer{T}"/> instance and writes them into a target <see cref="Span{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffer.</typeparam>
    /// <param name="source">The input <see cref="Buffer{T}"/> instance to read data from.</param>
    /// <param name="destination">The input <see cref="Span{T}"/> to write data to.</param>
    /// <param name="sourceOffset">The offset to start reading data from.</param>
    public static void CopyTo<T>(this Buffer<T> source, Span<T> destination, int sourceOffset)
        where T : unmanaged
    {
        default(ArgumentNullException).ThrowIfNull(source);

        source.CopyTo(ref MemoryMarshal.GetReference(destination), sourceOffset, destination.Length);
    }

    /// <summary>
    /// Reads the contents of the specified <see cref="Buffer{T}"/> instance and copies them to the target <see cref="Buffer{T}"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffers.</typeparam>
    /// <param name="source">The input <see cref="Buffer{T}"/> instance to read data from.</param>
    /// <param name="destination">The target <see cref="Buffer{T}"/> instance to write data to.</param>
    public static void CopyTo<T>(this Buffer<T> source, Buffer<T> destination)
        where T : unmanaged
    {
        default(ArgumentNullException).ThrowIfNull(source);
        default(ArgumentNullException).ThrowIfNull(destination);

        source.CopyTo(destination, 0, 0, source.Length);
    }

    /// <summary>
    /// Reads the contents of the specified range from a <see cref="Buffer{T}"/> instance and copies them to the target <see cref="Buffer{T}"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffers.</typeparam>
    /// <param name="source">The input <see cref="Buffer{T}"/> instance to read data from.</param>
    /// <param name="destination">The target <see cref="Buffer{T}"/> instance to write data to.</param>
    /// <param name="sourceOffset">The offset to start reading data from.</param>
    /// <param name="destinationOffset">The starting offset within <paramref name="destination"/> to write data to.</param>
    /// <param name="count">The number of items to read.</param>
    public static void CopyTo<T>(this Buffer<T> source, Buffer<T> destination, int sourceOffset, int destinationOffset, int count)
        where T : unmanaged
    {
        default(ArgumentNullException).ThrowIfNull(source);
        default(ArgumentNullException).ThrowIfNull(destination);

        source.CopyTo(destination, sourceOffset, destinationOffset, count);
    }

    /// <summary>
    /// Writes the contents of a given <typeparamref name="T"/> array to a <see cref="Buffer{T}"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffer.</typeparam>
    /// <param name="destination">The target <see cref="Buffer{T}"/> instance to write data to.</param>
    /// <param name="source">The input <typeparamref name="T"/> array to read data from.</param>
    public static void CopyFrom<T>(this Buffer<T> destination, T[] source)
        where T : unmanaged
    {
        default(ArgumentNullException).ThrowIfNull(destination);
        default(ArgumentNullException).ThrowIfNull(source);

        destination.CopyFrom(source.AsSpan(), 0);
    }

    /// <summary>
    /// Writes the contents of a given <typeparamref name="T"/> array to a specified area of a <see cref="Buffer{T}"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffer.</typeparam>
    /// <param name="destination">The target <see cref="Buffer{T}"/> instance to write data to.</param>
    /// <param name="source">The input <typeparamref name="T"/> array to read data from.</param>
    /// <param name="sourceOffset">The starting offset within <paramref name="source"/> to read data from.</param>
    /// <param name="destinationOffset">The offset to start writing data to.</param>
    /// <param name="count">The number of items to write.</param>
    public static void CopyFrom<T>(this Buffer<T> destination, T[] source, int sourceOffset, int destinationOffset, int count)
        where T : unmanaged
    {
        default(ArgumentNullException).ThrowIfNull(destination);
        default(ArgumentNullException).ThrowIfNull(source);

        ReadOnlySpan<T> span = source.AsSpan(sourceOffset, count);

        destination.CopyFrom(span, destinationOffset);
    }

    /// <summary>
    /// Writes the contents of a given <see cref="ReadOnlySpan{T}"/> to a <see cref="Buffer{T}"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffer.</typeparam>
    /// <param name="destination">The target <see cref="Buffer{T}"/> instance to write data to.</param>
    /// <param name="source">The input <see cref="ReadOnlySpan{T}"/> to read data from.</param>
    public static void CopyFrom<T>(this Buffer<T> destination, ReadOnlySpan<T> source)
        where T : unmanaged
    {
        default(ArgumentNullException).ThrowIfNull(destination);

        destination.CopyFrom(source, 0);
    }

    /// <summary>
    /// Writes the contents of a given <see cref="ReadOnlySpan{T}"/> to a specified area of a <see cref="Buffer{T}"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffer.</typeparam>
    /// <param name="destination">The target <see cref="Buffer{T}"/> instance to write data to.</param>
    /// <param name="source">The input <see cref="ReadOnlySpan{T}"/> to read data from.</param>
    /// <param name="destinationOffset">The offset to start writing data to.</param>
    public static void CopyFrom<T>(this Buffer<T> destination, ReadOnlySpan<T> source, int destinationOffset)
        where T : unmanaged
    {
        default(ArgumentNullException).ThrowIfNull(destination);

        destination.CopyFrom(ref MemoryMarshal.GetReference(source), destinationOffset, source.Length);
    }

    /// <summary>
    /// Writes the contents of the specified <see cref="Buffer{T}"/> instance to the current <see cref="Buffer{T}"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffers.</typeparam>
    /// <param name="destination">The input <see cref="Buffer{T}"/> instance to write data data to.</param>
    /// <param name="source">The source <see cref="Buffer{T}"/> instance to read data from.</param>
    public static void CopyFrom<T>(this Buffer<T> destination, Buffer<T> source)
        where T : unmanaged
    {
        default(ArgumentNullException).ThrowIfNull(destination);
        default(ArgumentNullException).ThrowIfNull(source);

        source.CopyTo(destination, 0, 0, source.Length);
    }

    /// <summary>
    /// Writes the contents of the specified range from a <see cref="Buffer{T}"/> instance and copies them to the current <see cref="Buffer{T}"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of items stored on the buffers.</typeparam>
    /// <param name="destination">The input <see cref="Buffer{T}"/> instance to write data to.</param>
    /// <param name="source">The source <see cref="Buffer{T}"/> instance to read data from.</param>
    /// <param name="sourceOffset">The offset to start reading data from.</param>
    /// <param name="destinationOffset">The starting offset within <paramref name="destination"/> to write data to.</param>
    /// <param name="count">The number of items to read.</param>
    public static void CopyFrom<T>(this Buffer<T> destination, Buffer<T> source, int sourceOffset, int destinationOffset, int count)
        where T : unmanaged
    {
        default(ArgumentNullException).ThrowIfNull(destination);
        default(ArgumentNullException).ThrowIfNull(source);

        source.CopyTo(destination, sourceOffset, destinationOffset, count);
    }
}
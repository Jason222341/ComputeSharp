using System;
using System.Runtime.InteropServices;
using ComputeSharp.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ComputeSharp.Tests.Extensions;

/// <summary>
/// A helper class for loading images into resources.
/// </summary>
public static class ImagingExtensions
{
    /// <summary>
    /// Creates a new <see cref="Image{TPixel}"/> instance with the specified texture data.
    /// </summary>
    /// <typeparam name="TFrom">The input pixel format used in the texture.</typeparam>
    /// <typeparam name="TTo">The target pixel format for the returned image.</typeparam>
    /// <param name="texture">The source <see cref="Texture2D{T}"/> instance to read data from.</param>
    /// <returns>An image with the data from the input texture.</returns>
    public static unsafe Image<TTo> ToImage<TFrom, TTo>(this Texture2D<TFrom> texture)
        where TFrom : unmanaged
        where TTo : unmanaged, IPixel<TTo>
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(sizeof(TTo), sizeof(TFrom), nameof(TTo));

        Image<TTo> image = new(texture.Width, texture.Height);

        Assert.IsTrue(image.DangerousTryGetSinglePixelMemory(out Memory<TTo> memory));

        Span<TFrom> pixels = MemoryMarshal.Cast<TTo, TFrom>(memory.Span);

        texture.CopyTo(pixels);

        return image;
    }

    /// <summary>
    /// Creates a new <see cref="Image{TPixel}"/> instance with the specified texture data.
    /// </summary>
    /// <typeparam name="TFrom">The input pixel format used in the texture.</typeparam>
    /// <typeparam name="TTo">The target pixel format for the returned image.</typeparam>
    /// <param name="texture">The source <see cref="Texture3D{T}"/> instance to read data from.</param>
    /// <param name="depth">The depth layer to read the image from.</param>
    /// <returns>An image with the data from the input texture at a specified depth layer.</returns>
    public static unsafe Image<TTo> ToImage<TFrom, TTo>(this Texture3D<TFrom> texture, int depth)
        where TFrom : unmanaged
        where TTo : unmanaged, IPixel<TTo>
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(sizeof(TTo), sizeof(TFrom), nameof(TTo));

        Image<TTo> image = new(texture.Width, texture.Height);

        Assert.IsTrue(image.DangerousTryGetSinglePixelMemory(out Memory<TTo> memory));

        Span<TFrom> pixels = MemoryMarshal.Cast<TTo, TFrom>(memory.Span);

        texture.CopyTo(pixels, 0, 0, depth, texture.Width, texture.Height, 1);

        return image;
    }
}
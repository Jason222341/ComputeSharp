using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using ComputeSharp.Core.Extensions;
using ComputeSharp.Graphics.Extensions;
using ComputeSharp.Win32;

#pragma warning disable CA1416

namespace ComputeSharp.Graphics.Helpers;

/// <summary>
/// A <see langword="class"/> that uses the WIC APIs to load and decode images.
/// </summary>
internal sealed unsafe class WICHelper
{
    /// <summary>
    /// The <see cref="IWICImagingFactory2"/> instance to use to create decoders.
    /// </summary>
    private readonly ComPtr<IWICImagingFactory2> wicImagingFactory2;

    /// <summary>
    /// Creates a new <see cref="WICHelper"/> instance.
    /// </summary>
    private WICHelper()
    {
        using ComPtr<IWICImagingFactory2> wicImagingFactory2 = default;

        Windows.CoCreateInstance(
            (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in CLSID.CLSID_WICImagingFactory2)),
            null,
            (uint)CLSCTX.CLSCTX_INPROC_SERVER,
            Windows.__uuidof<IWICImagingFactory2>(),
            (void**)wicImagingFactory2.GetAddressOf()).Assert();

        this.wicImagingFactory2 = wicImagingFactory2.Move();
    }

    /// <summary>
    /// Destroys the current <see cref="WICHelper"/> instance.
    /// </summary>
    ~WICHelper()
    {
        this.wicImagingFactory2.Dispose();
    }

    /// <summary>
    /// Gets a <see cref="WICHelper"/> instance to use.
    /// </summary>
    public static WICHelper Instance { get; } = new();

    /// <summary>
    /// Loads an <see cref="UploadTexture2D{T}"/> from a specified file.
    /// </summary>
    /// <typeparam name="T">The type of items to store in the texture.</typeparam>
    /// <param name="device">The <see cref="GraphicsDevice"/> instance to use to allocate the texture.</param>
    /// <param name="filename">The filename of the image file to load and decode into the texture.</param>
    /// <returns>An <see cref="UploadTexture2D{T}"/> instance with the contents of the specified file.</returns>
    public UploadTexture2D<T> LoadTexture<T>(GraphicsDevice device, ReadOnlySpan<char> filename)
        where T : unmanaged
    {
        using ComPtr<IWICBitmapDecoder> wicBitmapDecoder = default;

        // Get the bitmap decoder for the target file
        fixed (char* p = filename)
        {
            this.wicImagingFactory2.Get()->CreateDecoderFromFilename(
                (ushort*)p,
                null,
                Windows.GENERIC_READ,
                WICDecodeOptions.WICDecodeMetadataCacheOnDemand,
                wicBitmapDecoder.GetAddressOf()).Assert();
        }

        return LoadTexture<T>(device, wicBitmapDecoder.Get());
    }

    /// <summary>
    /// Loads an <see cref="UploadTexture2D{T}"/> from a specified buffer.
    /// </summary>
    /// <typeparam name="T">The type of items to store in the texture.</typeparam>
    /// <param name="device">The <see cref="GraphicsDevice"/> instance to use to allocate the texture.</param>
    /// <param name="span">The buffer with the image data to load and decode into the texture.</param>
    /// <returns>An <see cref="UploadTexture2D{T}"/> instance with the contents of the specified file.</returns>
    public UploadTexture2D<T> LoadTexture<T>(GraphicsDevice device, ReadOnlySpan<byte> span)
        where T : unmanaged
    {
        using ComPtr<IWICBitmapDecoder> wicBitmapDecoder = default;
        using ComPtr<IWICStream> wicStream = default;

        // Get the bitmap decoder for the target buffer
        fixed (byte* p = span)
        {
            this.wicImagingFactory2.Get()->CreateStream(wicStream.GetAddressOf()).Assert();

            wicStream.Get()->InitializeFromMemory(p, (uint)span.Length).Assert();

            this.wicImagingFactory2.Get()->CreateDecoderFromStream(
                (IStream*)wicStream.Get(),
                null,
                WICDecodeOptions.WICDecodeMetadataCacheOnDemand,
                wicBitmapDecoder.GetAddressOf()).Assert();

            return LoadTexture<T>(device, wicBitmapDecoder.Get());
        }
    }

    /// <summary>
    /// Loads an <see cref="UploadTexture2D{T}"/> from a specified stream.
    /// </summary>
    /// <typeparam name="T">The type of items to store in the texture.</typeparam>
    /// <param name="device">The <see cref="GraphicsDevice"/> instance to use to allocate the texture.</param>
    /// <param name="stream">The stream with the image data to load and decode into the texture.</param>
    /// <returns>An <see cref="UploadTexture2D{T}"/> instance with the contents of the specified file.</returns>
    public UploadTexture2D<T> LoadTexture<T>(GraphicsDevice device, Stream stream)
        where T : unmanaged
    {
        using ComPtr<IWICBitmapDecoder> wicBitmapDecoder = default;
        using ComPtr<IWICStream> wicStream = default;

        this.wicImagingFactory2.Get()->CreateStream(wicStream.GetAddressOf()).Assert();

        wicStream.Get()->InitializeFromStream(stream).Assert();

        this.wicImagingFactory2.Get()->CreateDecoderFromStream(
            (IStream*)wicStream.Get(),
            null,
            WICDecodeOptions.WICDecodeMetadataCacheOnDemand,
            wicBitmapDecoder.GetAddressOf()).Assert();

        return LoadTexture<T>(device, wicBitmapDecoder.Get());
    }

    /// <summary>
    /// Loads an <see cref="UploadTexture2D{T}"/> from a specified <see cref="IWICBitmapDecoder"/> object.
    /// </summary>
    /// <typeparam name="T">The type of items to store in the texture.</typeparam>
    /// <param name="device">The <see cref="GraphicsDevice"/> instance to use to allocate the texture.</param>
    /// <param name="wicBitmapDecoder">The <see cref="IWICBitmapDecoder"/> object in use.</param>
    /// <returns>An <see cref="UploadTexture2D{T}"/> instance with the contents of the specified file.</returns>
    private UploadTexture2D<T> LoadTexture<T>(GraphicsDevice device, IWICBitmapDecoder* wicBitmapDecoder)
        where T : unmanaged
    {
        using ComPtr<IWICBitmapFrameDecode> wicBitmapFrameDecode = default;

        // Get the first frame of the loaded image (if more are present, they will be ignored)
        wicBitmapDecoder->GetFrame(0, wicBitmapFrameDecode.GetAddressOf()).Assert();

        uint width;
        uint height;

        // Extract the image size info
        wicBitmapFrameDecode.Get()->GetSize(&width, &height).Assert();

        Guid wicTargetPixelFormatGuid = WICFormatHelper.GetForType<T>();
        Guid wicActualPixelFormatGuid;

        // Get the current and target pixel format info
        wicBitmapFrameDecode.Get()->GetPixelFormat(&wicActualPixelFormatGuid).Assert();

        // If the current pixel format is the same as the target one, we can just read the
        // decoded pixel data directly. Otherwise, we need a pixel format conversion step.
        if (wicTargetPixelFormatGuid == wicActualPixelFormatGuid)
        {
            // Allocate an upload texture to transfer the decoded pixel data
            UploadTexture2D<T> upload = device.AllocateUploadTexture2D<T>((int)width, (int)height);

            T* data = upload.View.DangerousGetAddressAndByteStride(out int strideInBytes);

            // Copy the decoded pixels directly from the loaded image
            wicBitmapFrameDecode.Get()->CopyPixels(
                prc: null,
                cbStride: (uint)strideInBytes,
                cbBufferSize: (uint)strideInBytes * height,
                pbBuffer: (byte*)data).Assert();

            return upload;
        }
        else
        {
            using ComPtr<IWICFormatConverter> wicFormatConverter = default;

            this.wicImagingFactory2.Get()->CreateFormatConverter(wicFormatConverter.GetAddressOf()).Assert();

            // Get a format converter to decode the pixel data
            wicFormatConverter.Get()->Initialize(
                (IWICBitmapSource*)wicBitmapFrameDecode.Get(),
                &wicTargetPixelFormatGuid,
                WICBitmapDitherType.WICBitmapDitherTypeNone,
                null,
                0,
                WICBitmapPaletteType.WICBitmapPaletteTypeMedianCut).Assert();

            // Allocate an upload texture to transfer the converted pixel data
            UploadTexture2D<T> upload = device.AllocateUploadTexture2D<T>((int)width, (int)height);

            T* data = upload.View.DangerousGetAddressAndByteStride(out int strideInBytes);

            // Decode the pixel data into the upload buffer
            wicFormatConverter.Get()->CopyPixels(
                prc: null,
                cbStride: (uint)strideInBytes,
                cbBufferSize: (uint)strideInBytes * height,
                pbBuffer: (byte*)data).Assert();

            return upload;
        }
    }

    /// <summary>
    /// Loads an image from a specified file.
    /// </summary>
    /// <typeparam name="T">The type of items to store in the texture.</typeparam>
    /// <param name="texture">The target <see cref="TextureView2D{T}"/> instance to write data to.</param>
    /// <param name="filename">The filename of the image file to load and decode into the texture.</param>
    public void LoadTexture<T>(TextureView2D<T> texture, ReadOnlySpan<char> filename)
        where T : unmanaged
    {
        using ComPtr<IWICBitmapDecoder> wicBitmapDecoder = default;

        fixed (char* p = filename)
        {
            this.wicImagingFactory2.Get()->CreateDecoderFromFilename(
                (ushort*)p,
                null,
                Windows.GENERIC_READ,
                WICDecodeOptions.WICDecodeMetadataCacheOnDemand,
                wicBitmapDecoder.GetAddressOf()).Assert();
        }

        LoadTexture(texture, wicBitmapDecoder.Get());
    }

    /// <summary>
    /// Loads an image from a specified buffer.
    /// </summary>
    /// <typeparam name="T">The type of items to store in the texture.</typeparam>
    /// <param name="texture">The target <see cref="TextureView2D{T}"/> instance to write data to.</param>
    /// <param name="span">The buffer with the image data to load and decode into the texture.</param>
    public void LoadTexture<T>(TextureView2D<T> texture, ReadOnlySpan<byte> span)
        where T : unmanaged
    {
        using ComPtr<IWICBitmapDecoder> wicBitmapDecoder = default;
        using ComPtr<IWICStream> wicStream = default;

        // Get the bitmap decoder for the target buffer
        fixed (byte* p = span)
        {
            this.wicImagingFactory2.Get()->CreateStream(wicStream.GetAddressOf()).Assert();

            wicStream.Get()->InitializeFromMemory(p, (uint)span.Length).Assert();

            this.wicImagingFactory2.Get()->CreateDecoderFromStream(
                (IStream*)wicStream.Get(),
                null,
                WICDecodeOptions.WICDecodeMetadataCacheOnDemand,
                wicBitmapDecoder.GetAddressOf()).Assert();

            LoadTexture(texture, wicBitmapDecoder.Get());
        }
    }

    /// <summary>
    /// Loads an image from a specified stream.
    /// </summary>
    /// <typeparam name="T">The type of items to store in the texture.</typeparam>
    /// <param name="texture">The target <see cref="TextureView2D{T}"/> instance to write data to.</param>
    /// <param name="stream">The stream with the image data to load and decode into the texture.</param>
    public void LoadTexture<T>(TextureView2D<T> texture, Stream stream)
        where T : unmanaged
    {
        using ComPtr<IWICBitmapDecoder> wicBitmapDecoder = default;
        using ComPtr<IWICStream> wicStream = default;

        this.wicImagingFactory2.Get()->CreateStream(wicStream.GetAddressOf()).Assert();

        wicStream.Get()->InitializeFromStream(stream).Assert();

        this.wicImagingFactory2.Get()->CreateDecoderFromStream(
            (IStream*)wicStream.Get(),
            null,
            WICDecodeOptions.WICDecodeMetadataCacheOnDemand,
            wicBitmapDecoder.GetAddressOf()).Assert();

        LoadTexture(texture, wicBitmapDecoder.Get());
    }

    /// <summary>
    /// Loads image data from a specified <see cref="IWICBitmapDecoder"/> object into a target texture.
    /// </summary>
    /// <typeparam name="T">The type of items to store in the texture.</typeparam>
    /// <param name="texture">The target <see cref="TextureView2D{T}"/> instance to write data to.</param>
    /// <param name="wicBitmapDecoder">The <see cref="IWICBitmapDecoder"/> object in use.</param>
    private void LoadTexture<T>(TextureView2D<T> texture, IWICBitmapDecoder* wicBitmapDecoder)
        where T : unmanaged
    {
        using ComPtr<IWICBitmapFrameDecode> wicBitmapFrameDecode = default;

        wicBitmapDecoder->GetFrame(0, wicBitmapFrameDecode.GetAddressOf()).Assert();

        uint width;
        uint height;

        // Extract the image size info
        wicBitmapFrameDecode.Get()->GetSize(&width, &height).Assert();

        // Validate the target texture has the right size
        default(ArgumentException).ThrowIf((uint)texture.Width != width, nameof(texture.Width));
        default(ArgumentException).ThrowIf((uint)texture.Height != height, nameof(texture.Height));

        Guid wicTargetPixelFormatGuid = WICFormatHelper.GetForType<T>();
        Guid wicActualPixelFormatGuid;

        wicBitmapFrameDecode.Get()->GetPixelFormat(&wicActualPixelFormatGuid).Assert();

        T* data = texture.DangerousGetAddressAndByteStride(out int strideInBytes);

        // Copy the decoded pixels directly from the loaded image
        if (wicTargetPixelFormatGuid == wicActualPixelFormatGuid)
        {
            wicBitmapFrameDecode.Get()->CopyPixels(
                prc: null,
                cbStride: (uint)strideInBytes,
                cbBufferSize: (uint)strideInBytes * height,
                pbBuffer: (byte*)data).Assert();
        }
        else
        {
            using ComPtr<IWICFormatConverter> wicFormatConverter = default;

            this.wicImagingFactory2.Get()->CreateFormatConverter(wicFormatConverter.GetAddressOf()).Assert();

            wicFormatConverter.Get()->Initialize(
                (IWICBitmapSource*)wicBitmapFrameDecode.Get(),
                &wicTargetPixelFormatGuid,
                WICBitmapDitherType.WICBitmapDitherTypeNone,
                null,
                0,
                WICBitmapPaletteType.WICBitmapPaletteTypeMedianCut).Assert();

            // Decode the pixel data into the upload buffer
            wicFormatConverter.Get()->CopyPixels(
                prc: null,
                cbStride: (uint)strideInBytes,
                cbBufferSize: (uint)strideInBytes * height,
                pbBuffer: (byte*)data).Assert();
        }
    }

    /// <summary>
    /// Saves a texture to a specified file.
    /// </summary>
    /// <typeparam name="T">The type of items to store in the texture.</typeparam>
    /// <param name="texture">The texture data to save to an image.</param>
    /// <param name="filename">The filename of the image file to save.</param>
    public void SaveTexture<T>(TextureView2D<T> texture, ReadOnlySpan<char> filename)
        where T : unmanaged
    {
        using ComPtr<IWICBitmapEncoder> wicBitmapEncoder = default;

        Guid containerGuid = WICFormatHelper.GetForFilename(filename);

        // Create the image encoder
        this.wicImagingFactory2.Get()->CreateEncoder(
            &containerGuid,
            null,
            wicBitmapEncoder.GetAddressOf()).Assert();

        using ComPtr<IWICStream> wicStream = default;

        // Create and initialize a stream to the target file
        this.wicImagingFactory2.Get()->CreateStream(wicStream.GetAddressOf()).Assert();

        fixed (char* p = filename)
        {
            _ = wicStream.Get()->InitializeFromFilename(
                (ushort*)p,
                Windows.GENERIC_WRITE);
        }

        // Initialize the encoder
        wicBitmapEncoder.Get()->Initialize(
            (IStream*)wicStream.Get(),
            WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache).Assert();

        SaveTexture(texture, wicBitmapEncoder.Get(), containerGuid);
    }

    /// <summary>
    /// Saves a texture to a specified stream.
    /// </summary>
    /// <typeparam name="T">The type of items to store in the texture.</typeparam>
    /// <param name="texture">The texture data to save to a stream.</param>
    /// <param name="stream">The target stream to write to.</param>
    /// <param name="format">The target image format to use.</param>
    public void SaveTexture<T>(TextureView2D<T> texture, Stream stream, ImageFormat format)
        where T : unmanaged
    {
        using ComPtr<IWICBitmapEncoder> wicBitmapEncoder = default;

        Guid containerGuid = WICFormatHelper.GetForFormat(format);

        // Create the image encoder
        this.wicImagingFactory2.Get()->CreateEncoder(
            &containerGuid,
            null,
            wicBitmapEncoder.GetAddressOf()).Assert();

        using ComPtr<IWICStream> wicStream = default;

        // Create and initialize a stream
        this.wicImagingFactory2.Get()->CreateStream(wicStream.GetAddressOf()).Assert();

        _ = wicStream.Get()->InitializeFromStream(stream);

        // Initialize the encoder
        wicBitmapEncoder.Get()->Initialize(
            (IStream*)wicStream.Get(),
            WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache).Assert();

        SaveTexture(texture, wicBitmapEncoder.Get(), containerGuid);
    }

    /// <summary>
    /// Saves a texture to a specified buffer writer.
    /// </summary>
    /// <typeparam name="T">The type of items to store in the texture.</typeparam>
    /// <param name="texture">The texture data to save to a stream.</param>
    /// <param name="writer">The target buffer writer to write to.</param>
    /// <param name="format">The target image format to use.</param>
    public void SaveTexture<T>(TextureView2D<T> texture, IBufferWriter<byte> writer, ImageFormat format)
        where T : unmanaged
    {
        using ComPtr<IWICBitmapEncoder> wicBitmapEncoder = default;

        Guid containerGuid = WICFormatHelper.GetForFormat(format);

        // Create the image encoder
        this.wicImagingFactory2.Get()->CreateEncoder(
            &containerGuid,
            null,
            wicBitmapEncoder.GetAddressOf()).Assert();

        using ComPtr<IWICStream> wicStream = default;

        // Create and initialize a stream
        this.wicImagingFactory2.Get()->CreateStream(wicStream.GetAddressOf()).Assert();

        _ = wicStream.Get()->InitializeFromBufferWriter(writer);

        // Initialize the encoder
        wicBitmapEncoder.Get()->Initialize(
            (IStream*)wicStream.Get(),
            WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache).Assert();

        SaveTexture(texture, wicBitmapEncoder.Get(), containerGuid);
    }

    /// <summary>
    /// Saves a texture to a specified <see cref="IWICBitmapEncoder"/> object.
    /// </summary>
    /// <typeparam name="T">The type of items to store in the texture.</typeparam>
    /// <param name="texture">The texture data to save to an image.</param>
    /// <param name="wicBitmapEncoder">The <see cref="IWICBitmapEncoder"/> object in use.</param>
    /// <param name="format">The <see cref="Guid"/> identifying the target format.</param>
    private void SaveTexture<T>(TextureView2D<T> texture, IWICBitmapEncoder* wicBitmapEncoder, Guid format)
        where T : unmanaged
    {
        using ComPtr<IWICBitmapFrameEncode> wicBitmapFrameEncode = default;

        // Create the image frame and initialize it
        wicBitmapEncoder->CreateNewFrame(wicBitmapFrameEncode.GetAddressOf(), null).Assert();

        wicBitmapFrameEncode.Get()->Initialize(null).Assert();
        wicBitmapFrameEncode.Get()->SetSize((uint)texture.Width, (uint)texture.Height).Assert();

        // Depending on the target format and the current pixel type, we need to check whether
        // an intermediate encoding step is necessary. This is because not all pixel formats
        // are directly supported by the native image encoders present in Windows.
        if (WICFormatHelper.TryGetIntermediateFormatForType<T>(format, out Guid intermediateGuid))
        {
            T* data = texture.DangerousGetAddressAndByteStride(out int strideInBytes);

            using ComPtr<IWICBitmap> wicBitmap = default;
            Guid initialGuid = WICFormatHelper.GetForType<T>();

            // Create a bitmap wrapping the input texture
            this.wicImagingFactory2.Get()->CreateBitmapFromMemory(
                (uint)texture.Width,
                (uint)texture.Height,
                &initialGuid,
                (uint)strideInBytes,
                (uint)(strideInBytes * texture.Height),
                (byte*)data,
                wicBitmap.GetAddressOf()).Assert();

            using ComPtr<IWICFormatConverter> wicFormatConverter = default;

            this.wicImagingFactory2.Get()->CreateFormatConverter(wicFormatConverter.GetAddressOf()).Assert();

            // Get a format converter to encode the pixel data
            wicFormatConverter.Get()->Initialize(
                (IWICBitmapSource*)wicBitmap.Get(),
                &intermediateGuid,
                WICBitmapDitherType.WICBitmapDitherTypeNone,
                null,
                0,
                WICBitmapPaletteType.WICBitmapPaletteTypeMedianCut).Assert();

            // Write the encoded image data to the frame
            wicBitmapFrameEncode.Get()->SetPixelFormat(&intermediateGuid).Assert();
            wicBitmapFrameEncode.Get()->WriteSource(
                (IWICBitmapSource*)wicBitmap.Get(),
                null).Assert();
        }
        else
        {
            T* data = texture.DangerousGetAddressAndByteStride(out int strideInBytes);
            Guid initialGuid = WICFormatHelper.GetForType<T>();

            // Write the image data to the frame
            wicBitmapFrameEncode.Get()->SetPixelFormat(&initialGuid).Assert();
            wicBitmapFrameEncode.Get()->WritePixels(
                lineCount: (uint)texture.Height,
                cbStride: (uint)strideInBytes,
                cbBufferSize: (uint)(strideInBytes * texture.Height),
                pbPixels: (byte*)data).Assert();
        }

        _ = wicBitmapFrameEncode.Get()->Commit();
        _ = wicBitmapEncoder->Commit();
    }
}
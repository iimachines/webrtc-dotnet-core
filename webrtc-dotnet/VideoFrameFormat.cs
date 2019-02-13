namespace WonderMediaProductions.WebRtc
{
    /// <summary>
    /// Pixel formats that are supported by the encoder.
    /// </summary>
    /// <remarks>
    /// Currently if a hardware encoder is found,
    /// you must pass <see cref="CpuTexture"/>,
    /// and the format must be Bgra32.
    ///
    /// The software encoder on the other hand does not support <see cref="CpuTexture"/>
    ///
    /// This of course should be refactored at some point,
    /// since although high-performance, it is not user friendly.
    /// </remarks>
    public enum VideoFrameFormat
    {
        /// <summary>
        /// Software encoder pixel format, 8-bit R G B A in system memory
        /// </summary>
        Rgba32,

        /// <summary>
        /// Software encoder pixel format,  8-bit B G R A in system memory 
        /// </summary>
        Bgra32,

        /// <summary>
        /// Software encoder pixel format, 8-bit A R G B in system memory 
        /// </summary>
        Argb32,

        /// <summary>
        /// Software encoder pixel format, 8-bit A B G R in system memory 
        /// </summary>
        Abgr32,

        /// <summary>
        /// Hardware encoder pixel format, 8-bit B G R A in system memory 
        /// </summary>
        /// <remarks>
        /// The texture must reside in CPU memory, 
        /// and is just a plain pointer to the first pixel
        /// </remarks>
        CpuTexture,

        /// <summary>
        /// Hardware encoder pixel format, 8-bit B G R A, D3D11 Texture2D in GPU host memory
        /// </summary>
        GpuTextureD3D11,
    };
}
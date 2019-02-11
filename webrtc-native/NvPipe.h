#pragma once
#include "macros.h"

extern "C" {
    class ID3D11Texture2D;
}

namespace NvPipe
{
    extern "C" {

        enum class Codec
        {
            H264,
            HEVC
        };

        enum class Compression
        {
            LOSSY,
            LOSSLESS
        };

        enum class Format
        {
            BGRA32,
            UINT4,
            UINT8,
            UINT16,
            UINT32
        };

        class Instance;

        typedef Instance* CreateEncoder(Format format, Codec codec, Compression compression, uint64_t bitrate, uint32_t targetFrameRate);
        typedef void SetBitrate(Instance* nvp, uint64_t bitrate, uint32_t targetFrameRate);
        typedef uint64_t Encode(Instance* nvp, const void* src, uint64_t srcPitch, uint8_t* dst, uint64_t dstSize, uint32_t width, uint32_t height, bool forceIFrame);
        typedef uint64_t EncodeTextureD3D11(Instance* nvp, ID3D11Texture2D* texture, uint8_t* dst, uint64_t dstSize, bool forceIFrame);
        typedef void Destroy(Instance* nvp);
        typedef const char* GetError(Instance* nvp);
    }

    class Library
    {
    public:
        Library();
        ~Library();

        CreateEncoder* CreateEncoder = nullptr;
        Destroy* Destroy = nullptr;
        GetError* GetError = nullptr;

        SetBitrate* SetBitrate = nullptr;
        Encode* Encode = nullptr;
        EncodeTextureD3D11* EncodeTextureD3D11 = nullptr;

        bool IsAvailable() const { return is_available_; }

        DISALLOW_COPY_MOVE_ASSIGN(Library);

    private:
#ifdef _WIN32
        HMODULE module_ = nullptr;
#endif
        bool is_available_ = false;
    };
}

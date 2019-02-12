#include "pch.h"
#include "NativeVideoBuffer.h"

namespace webrtc
{
    NativeVideoBuffer::NativeVideoBuffer(
        int track_id,
        VideoFrameId frame_id,
        VideoFrameFormat format,
        int width, 
        int height, 
        const void* texture, 
        VideoFrameEvents* events)
        : track_id_(track_id)
        , frame_id_(frame_id)
        , format_(format)
        , width_(width)
        , height_(height)
        , texture_(texture)
        , events_(events)
    {
        if (texture_ && format_ == VideoFrameFormat::GpuTextureD3D11)
        {
            // Make sure to keep the texture alive until we're done with it.
            auto texture3D11 = reinterpret_cast<ID3D11Texture1D*>(const_cast<void*>(texture_));
            texture3D11->AddRef();
        }
    }

    NativeVideoBuffer::~NativeVideoBuffer()
    {
        if (events_ && texture_)
        {
            events_->OnFrameEncoded(track_id_, frame_id_, texture_);
        }

        if (texture_ && format_ == VideoFrameFormat::GpuTextureD3D11)
        {
            // Release the D3D11 texture when we're done with it.
            auto texture3D11 = reinterpret_cast<ID3D11Texture1D*>(const_cast<void*>(texture_));
            texture3D11->Release();
        }
    }

    VideoFrameBuffer::Type NativeVideoBuffer::type() const
    {
        return Type::kNative;
    }

    int NativeVideoBuffer::width() const
    {
        return width_;
    }

    int NativeVideoBuffer::height() const
    {
        return height_;
    }

    rtc::scoped_refptr<I420BufferInterface> NativeVideoBuffer::ToI420()
    {
        throw std::runtime_error("Converting a native buffer to a CPU 420 buffer is not supported");
    }
} // namespace webrtc

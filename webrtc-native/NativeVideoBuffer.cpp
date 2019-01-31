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
    }

    NativeVideoBuffer::~NativeVideoBuffer()
    {
        if (events_ && texture_)
        {
            events_->OnFrameEncoded(track_id_, frame_id_, texture_);
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

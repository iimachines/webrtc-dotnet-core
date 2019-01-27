#include "pch.h"
#include "NativeVideoBuffer.h"

namespace webrtc
{
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
        // TODO: Implement
        RTC_LOG(LS_ERROR) << "Converting a native buffer to I420 is not supported yet!";
        rtc::scoped_refptr<I420Buffer> buffer = I420Buffer::Create(width_, height_);
        I420Buffer::SetBlack(buffer);
        return buffer;
    }
} // namespace webrtc

#include "pch.h"
#include "VideoObserver.h"
#include "NativeVideoBuffer.h"

void VideoObserver::SetVideoCallback(IncomingVideoFrameCallback callback)
{
    std::lock_guard<std::mutex> lock(mutex);
    OnIncomingVideoFrame = callback;
}

void VideoObserver::OnFrame(const webrtc::VideoFrame& frame)
{
    std::unique_lock<std::mutex> lock(mutex);
    if (!OnIncomingVideoFrame)
        return;

    rtc::scoped_refptr<webrtc::VideoFrameBuffer> buffer(
        frame.video_frame_buffer());

    switch (buffer->type())
    {
    case webrtc::VideoFrameBuffer::Type::kI420A:
    {
        // The buffer has alpha channel.
        webrtc::I420ABufferInterface* i420a_buffer = buffer->GetI420A();

        OnIncomingVideoFrame(nullptr,
            i420a_buffer->DataY(), i420a_buffer->DataU(),
            i420a_buffer->DataV(), i420a_buffer->DataA(),
            i420a_buffer->StrideY(), i420a_buffer->StrideU(),
            i420a_buffer->StrideV(), i420a_buffer->StrideA(),
            frame.width(), frame.height(), frame.timestamp_us());
    }
    break;

    case webrtc::VideoFrameBuffer::Type::kNative:
    {
        const auto native_buffer = dynamic_cast<webrtc::NativeVideoBuffer*>(buffer.get());
        if (native_buffer)
        {
            OnIncomingVideoFrame(native_buffer->texture(),
                nullptr, nullptr, nullptr, nullptr,
                0, 0, 0, 0,
                frame.width(), frame.height(), frame.timestamp_us());
            break;
        }
        // goto default:
    }

    default:
    {
        rtc::scoped_refptr<webrtc::I420BufferInterface> i420_buffer = buffer->ToI420();
        OnIncomingVideoFrame(nullptr,
            i420_buffer->DataY(), i420_buffer->DataU(),
            i420_buffer->DataV(), nullptr, i420_buffer->StrideY(),
            i420_buffer->StrideU(), i420_buffer->StrideV(), 0,
            frame.width(), frame.height(), frame.timestamp_us());
    }
    break;
    }
}

#include "pch.h"
#include "VideoObserver.h"

void VideoObserver::SetVideoCallback(I420FRAMEREADY_CALLBACK callback)
{
    std::lock_guard<std::mutex> lock(mutex);
    OnI420FrameReady = callback;
}

void VideoObserver::OnFrame(const webrtc::VideoFrame& frame)
{
    std::unique_lock<std::mutex> lock(mutex);
    if (!OnI420FrameReady)
        return;

    rtc::scoped_refptr<webrtc::VideoFrameBuffer> buffer(
        frame.video_frame_buffer());

    if (buffer->type() != webrtc::VideoFrameBuffer::Type::kI420A)
    {
        rtc::scoped_refptr<webrtc::I420BufferInterface> i420_buffer =
            buffer->ToI420();
        OnI420FrameReady(i420_buffer->DataY(), i420_buffer->DataU(),
            i420_buffer->DataV(), nullptr, i420_buffer->StrideY(),
            i420_buffer->StrideU(), i420_buffer->StrideV(), 0,
            frame.width(), frame.height(), frame.timestamp_us());
    }
    else
    {
        // The buffer has alpha channel.
        webrtc::I420ABufferInterface* i420a_buffer = buffer->GetI420A();

        OnI420FrameReady(i420a_buffer->DataY(), i420a_buffer->DataU(),
            i420a_buffer->DataV(), i420a_buffer->DataA(),
            i420a_buffer->StrideY(), i420a_buffer->StrideU(),
            i420a_buffer->StrideV(), i420a_buffer->StrideA(),
            frame.width(), frame.height(), frame.timestamp_us());
    }
}

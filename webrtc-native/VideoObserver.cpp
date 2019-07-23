#include "pch.h"
#include "VideoObserver.h"
#include "NativeVideoBuffer.h"

void VideoObserver::SetVideoCallback(IncomingVideoFrameCallback callback)
{
    std::lock_guard<std::mutex> lock(mutex);
    OnIncomingVideoFrame = callback;
}

void VideoObserver::SetArgbVideoCallback(IncomingVideoFrameCallback callback)
{
    std::lock_guard<std::mutex> lock(mutex);
    OnIncomingArgbVideoFrame = callback;
}

void VideoObserver::OnFrame(const webrtc::VideoFrame& frame)
{
    std::unique_lock<std::mutex> lock(mutex);
    if (!OnIncomingVideoFrame && !OnIncomingArgbVideoFrame)
        return;

    const int width = frame.width();
    const int height = frame.height();
    rtc::scoped_refptr<webrtc::VideoFrameBuffer> buffer(frame.video_frame_buffer());
    if (buffer->type() == webrtc::VideoFrameBuffer::Type::kNative)
    {
        const auto native_buffer = dynamic_cast<webrtc::NativeVideoBuffer*>(buffer.get());
        if (native_buffer)
        {
            if (OnIncomingVideoFrame)
            {
                OnIncomingVideoFrame(native_buffer->texture(),
                    nullptr, nullptr, nullptr, nullptr,
                    0, 0, 0, 0,
                    width, height, frame.timestamp_us());
            }
            if (OnIncomingArgbVideoFrame)
            {
                OnIncomingArgbVideoFrame(native_buffer->texture(),
                    nullptr, nullptr, nullptr, nullptr,
                    0, 0, 0, 0,
                    width, height, frame.timestamp_us());
            }
            return;
        }
    }

    if (buffer->type() == webrtc::VideoFrameBuffer::Type::kI420A)
    {
        auto i420a_buffer = buffer->GetI420A();
        if (OnIncomingVideoFrame)
        {
            OnIncomingVideoFrame(nullptr,
                i420a_buffer->DataY(), i420a_buffer->DataU(),
                i420a_buffer->DataV(), i420a_buffer->DataA(),
                i420a_buffer->StrideY(), i420a_buffer->StrideU(),
                i420a_buffer->StrideV(), i420a_buffer->StrideA(),
                width, height, frame.timestamp_us());
        }
        if (OnIncomingArgbVideoFrame)
        {
            const int stride = width * 4;
            std::vector<byte> pixels(stride * height);
            libyuv::I420AlphaToARGB(i420a_buffer->DataY(), i420a_buffer->StrideY(),
                i420a_buffer->DataU(), i420a_buffer->StrideU(),
                i420a_buffer->DataV(), i420a_buffer->StrideV(),
                i420a_buffer->DataA(), i420a_buffer->StrideA(),
                pixels.data(), stride, width, height, 0);
            OnIncomingArgbVideoFrame(pixels.data(),
                nullptr, nullptr, nullptr, nullptr,
                0, 0, 0, 0,
                width, height, frame.timestamp_us());
        }
        return;
    }
    rtc::scoped_refptr<webrtc::I420BufferInterface> i420_buffer_holder;
    webrtc::I420BufferInterface* i420_buffer;
    if (buffer->type() == webrtc::VideoFrameBuffer::Type::kI420)
    {
        i420_buffer = buffer->GetI420();
    }
    else
    {
        i420_buffer_holder = buffer->ToI420();
        i420_buffer = i420_buffer_holder.get();
    }

    if (OnIncomingVideoFrame)
    {
        OnIncomingVideoFrame(nullptr,
            i420_buffer->DataY(), i420_buffer->DataU(),
            i420_buffer->DataV(), nullptr, i420_buffer->StrideY(),
            i420_buffer->StrideU(), i420_buffer->StrideV(), 0,
            width, height, frame.timestamp_us());
    }

    if (OnIncomingArgbVideoFrame)
    {
        const int stride = width * 4;
        std::vector<byte> pixels(stride * height);
        libyuv::I420ToARGB(i420_buffer->DataY(), i420_buffer->StrideY(),
            i420_buffer->DataU(), i420_buffer->StrideU(),
            i420_buffer->DataV(), i420_buffer->StrideV(),
            pixels.data(), stride, width, height);
        OnIncomingArgbVideoFrame(pixels.data(),
            nullptr, nullptr, nullptr, nullptr,
            0, 0, 0, 0,
            width, height, frame.timestamp_us());
    }
}

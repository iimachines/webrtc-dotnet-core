#pragma once
#include "macros.h"
#include "VideoFrameEvents.h"

namespace webrtc
{
    // TODO: Test!
    class NativeVideoBuffer : public VideoFrameBuffer
    {
    public:
        NativeVideoBuffer(int track_id, VideoFrameId frame_id, int width, int height, const void* texture, VideoFrameEvents* events);
        ~NativeVideoBuffer() override;

        Type type() const override;
        int width() const override;
        int height() const override;
        const void *texture() const { return texture_;  }

        DISALLOW_COPY_MOVE_ASSIGN(NativeVideoBuffer);

    private:
        rtc::scoped_refptr<I420BufferInterface> ToI420() override;

        const int track_id_;
        VideoFrameId frame_id_;
        const int width_;
        const int height_;
        const void* texture_;
        VideoFrameEvents* events_;
    };
} // namespace webrtc

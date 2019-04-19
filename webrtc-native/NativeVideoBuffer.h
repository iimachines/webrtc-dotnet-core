#pragma once
#include "macros.h"
#include "VideoFrameEvents.h"
#include "VideoObserver.h"

namespace webrtc
{

    class NativeVideoBuffer : public VideoFrameBuffer
    {
    public:
        NativeVideoBuffer(int track_id, VideoFrameFormat format, int width, int height, const void* texture, VideoFrameEvents* events);
        ~NativeVideoBuffer() override;

        Type type() const override;
        int width() const override;
        int height() const override;
        const void *texture() const { return texture_;  }
        VideoFrameFormat format() const { return format_; }

		bool is_encoded() const { return is_encoded_; }
        void set_encoded(bool is_encoded);

		// Delay between the request to encode the frame, and the actual encoding time point.
		std::chrono::microseconds request_encode_delay() const
        {
			return std::chrono::duration_cast<std::chrono::microseconds>(encoded_time_ - request_time_);
        }

        DISALLOW_COPY_MOVE_ASSIGN(NativeVideoBuffer);

    private:
        rtc::scoped_refptr<I420BufferInterface> ToI420() override;

        const int track_id_;
        VideoFrameFormat format_;
        const int width_;
        const int height_;
        const void* texture_;
        VideoFrameEvents* events_;
		bool is_encoded_ = false;
		std::chrono::time_point<std::chrono::high_resolution_clock> request_time_;
		std::chrono::time_point<std::chrono::high_resolution_clock> encoded_time_;
	};
} // namespace webrtc

#pragma once
#include <media/base/video_broadcaster.h>
#include <rtc_base/ref_counted_object.h>

namespace webrtc {

    // A minimal implementation of VideoTrackSource. 
    // Includes a VideoBroadcaster for injection of frames.
    class InjectableVideoTrackSource : public VideoTrackSource, public rtc::VideoSinkInterface<VideoFrame> {
    public:
        static rtc::scoped_refptr<InjectableVideoTrackSource> Create(bool is_screencast = false);

        bool is_screencast() const override { return is_screencast_; }

        void OnFrame(const VideoFrame& frame) override;

    protected:
        explicit InjectableVideoTrackSource(bool is_screencast = false);
        ~InjectableVideoTrackSource() override;

        rtc::VideoSourceInterface<VideoFrame>* source() override;

    private:
        const bool is_screencast_;
        rtc::VideoBroadcaster video_broadcaster_;
    };

}  // namespace webrtc

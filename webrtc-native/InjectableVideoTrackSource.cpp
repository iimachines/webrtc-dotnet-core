#include "pch.h"
#include "InjectableVideoTrackSource.h"

namespace webrtc
{
    rtc::scoped_refptr<InjectableVideoTrackSource> InjectableVideoTrackSource::Create(bool is_screencast)
    {
        return new rtc::RefCountedObject<InjectableVideoTrackSource>(is_screencast);
    }

    InjectableVideoTrackSource::InjectableVideoTrackSource(bool is_screencast)
        : VideoTrackSource(false /* remote */)
        , is_screencast_(is_screencast)
    {
    }

    InjectableVideoTrackSource::~InjectableVideoTrackSource() = default;

    rtc::VideoSourceInterface<VideoFrame>* InjectableVideoTrackSource::source()
    {
        return &video_broadcaster_;
    }

    void InjectableVideoTrackSource::OnFrame(const VideoFrame& frame)
    {
        video_broadcaster_.OnFrame(frame);
    }
} // namespace webrtc

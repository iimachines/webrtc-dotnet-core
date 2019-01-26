#include "pch.h"
#include "CapturerTrackSource.h"

#if 0
CapturerTrackSource::CapturerTrackSource(std::unique_ptr<VideoCameraCapturer> capturer)
    : VideoTrackSource(/*remote=*/false), capturer_(std::move(capturer))
{
}

rtc::scoped_refptr<CapturerTrackSource> CapturerTrackSource::Create()
{
    const size_t kWidth = 640;
    const size_t kHeight = 480;
    const size_t kFps = 30;
    const size_t kDeviceIndex = 0;
    std::unique_ptr<VideoCameraCapturer> capturer = absl::WrapUnique(
        VideoCameraCapturer::Create(kWidth, kHeight, kFps, kDeviceIndex));
    return capturer
               ? new rtc::RefCountedObject<CapturerTrackSource>(std::move(capturer))
               : nullptr;
}

#endif

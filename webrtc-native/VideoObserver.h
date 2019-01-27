#pragma once

#include "NativeInterface.h"

class VideoObserver final : public rtc::VideoSinkInterface<webrtc::VideoFrame>
{
public:
    VideoObserver() = default;
    ~VideoObserver() = default;

    void SetVideoCallback(I420FrameReadyCallback callback);

protected:
    // VideoSinkInterface implementation
    void OnFrame(const webrtc::VideoFrame& frame) override;

private:
    I420FrameReadyCallback OnI420FrameReady = nullptr;
    std::mutex mutex;
};

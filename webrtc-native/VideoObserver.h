#pragma once

#include "NativeInterface.h"

class VideoObserver final : public rtc::VideoSinkInterface<webrtc::VideoFrame>
{
public:
    VideoObserver() = default;
    ~VideoObserver() = default;

    void SetVideoCallback(I420FRAMEREADY_CALLBACK callback);

protected:
    // VideoSinkInterface implementation
    void OnFrame(const webrtc::VideoFrame& frame) override;

private:
    I420FRAMEREADY_CALLBACK OnI420FrameReady = nullptr;
    std::mutex mutex;
};

#pragma once

#include "NativeInterface.h"

class VideoObserver final : public rtc::VideoSinkInterface<webrtc::VideoFrame>
{
public:
    VideoObserver() = default;
    ~VideoObserver() = default;

    void SetVideoCallback(IncomingVideoFrameCallback callback);

protected:
    // VideoSinkInterface implementation
    void OnFrame(const webrtc::VideoFrame& frame) override;

private:
    IncomingVideoFrameCallback OnIncomingVideoFrame = nullptr;
    std::mutex mutex;
};

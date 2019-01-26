#pragma once

template<typename CAPTURER>
class CapturerTrackSource : public webrtc::VideoTrackSource
{
public:
    CapturerTrackSource(std::unique_ptr<CAPTURER>&& capturer, bool remote = false)
        : VideoTrackSource(remote)
        , capturer_(std::move(capturer))
    {
    }

    ~CapturerTrackSource()
    {
    }

private:
    rtc::VideoSourceInterface<webrtc::VideoFrame>* source() override
    {
        return capturer_.get();
    }

    std::unique_ptr<CAPTURER> capturer_;
};

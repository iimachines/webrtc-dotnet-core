#pragma once

class TestVideoCapturer : public rtc::VideoSourceInterface<webrtc::VideoFrame>
{
public:
    TestVideoCapturer();
    virtual ~TestVideoCapturer();

    void AddOrUpdateSink(rtc::VideoSinkInterface<webrtc::VideoFrame>* sink,
                         const rtc::VideoSinkWants& wants) override;
    void RemoveSink(rtc::VideoSinkInterface<webrtc::VideoFrame>* sink) override;

protected:
    void OnFrame(const webrtc::VideoFrame& frame);
    rtc::VideoSinkWants GetSinkWants();

private:
    void UpdateVideoAdapter();

    rtc::VideoBroadcaster broadcaster_;
    cricket::VideoAdapter video_adapter_;
};

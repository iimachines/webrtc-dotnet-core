#include "pch.h"

#include "TestVideoCapturer.h"

TestVideoCapturer::TestVideoCapturer() = default;
TestVideoCapturer::~TestVideoCapturer() = default;

void TestVideoCapturer::OnFrame(const webrtc::VideoFrame& frame)
{
    int cropped_width = 0;
    int cropped_height = 0;
    int out_width = 0;
    int out_height = 0;

    if (!video_adapter_.AdaptFrameResolution(
        frame.width(), frame.height(), frame.timestamp_us() * 1000,
        &cropped_width, &cropped_height, &out_width, &out_height))
    {
        // Drop frame in order to respect frame rate constraint.
        return;
    }

    if (out_height != frame.height() || out_width != frame.width())
    {
        // Video adapter has requested a down-scale. Allocate a new buffer and
        // return scaled version.
        rtc::scoped_refptr<webrtc::I420Buffer> scaled_buffer =
            webrtc::I420Buffer::Create(out_width, out_height);
        scaled_buffer->ScaleFrom(*frame.video_frame_buffer()->ToI420());
        broadcaster_.OnFrame(webrtc::VideoFrame::Builder()
                             .set_video_frame_buffer(scaled_buffer)
                             .set_rotation(webrtc::kVideoRotation_0)
                             .set_timestamp_us(frame.timestamp_us())
                             .set_id(frame.id())
                             .build());
    }
    else
    {
        // No adaptations needed, just return the frame as is.
        broadcaster_.OnFrame(frame);
    }
}

rtc::VideoSinkWants TestVideoCapturer::GetSinkWants()
{
    return broadcaster_.wants();
}

void TestVideoCapturer::AddOrUpdateSink(
    rtc::VideoSinkInterface<webrtc::VideoFrame>* sink,
    const rtc::VideoSinkWants& wants)
{
    broadcaster_.AddOrUpdateSink(sink, wants);
    UpdateVideoAdapter();
}

void TestVideoCapturer::RemoveSink(rtc::VideoSinkInterface<webrtc::VideoFrame>* sink)
{
    broadcaster_.RemoveSink(sink);
    UpdateVideoAdapter();
}

void TestVideoCapturer::UpdateVideoAdapter()
{
    rtc::VideoSinkWants wants = broadcaster_.wants();
    video_adapter_.OnResolutionFramerateRequest(
        wants.target_pixel_count, wants.max_pixel_count, wants.max_framerate_fps);
}

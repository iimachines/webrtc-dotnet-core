#pragma once

typedef int64_t VideoFrameId;

class VideoFrameEvents abstract
{
public:
    virtual ~VideoFrameEvents() = default;

    // Called when a video frame is encoded, and is ready to be reused.
    virtual void OnFrameEncoded(int video_track_id, VideoFrameId frame_id, const void* pixels) = 0;
};


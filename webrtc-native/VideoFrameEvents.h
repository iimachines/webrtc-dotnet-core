#pragma once

class VideoFrameEvents abstract
{
public:
    virtual ~VideoFrameEvents() = default;

    // Called when a video frame is processed, and is ready to be reused. It might not have been encoded, it could be skipped.
    virtual void OnFrameProcessed(int video_track_id, const void* pixels, bool was_encoded) = 0;
};


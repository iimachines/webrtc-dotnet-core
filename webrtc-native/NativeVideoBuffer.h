#pragma once

namespace webrtc
{
    // TODO: Test!
    class NativeVideoBuffer : public VideoFrameBuffer
    {
    public:
        NativeVideoBuffer(int width, int height, const void* texture) 
        : width_(width)
        , height_(height)
        , texture_(texture)
        {
        }

        Type type() const override;
        int width() const override;
        int height() const override;

    private:
        rtc::scoped_refptr<I420BufferInterface> ToI420() override;

        const int width_;
        const int height_;
        const void* texture_;
    };
} // namespace webrtc

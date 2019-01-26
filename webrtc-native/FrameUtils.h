#pragma once

namespace webrtc {
    class I420Buffer;
    class VideoFrame;
    class VideoFrameBuffer;
    namespace test {

        bool EqualPlane(const uint8_t* data1,
            const uint8_t* data2,
            int stride1,
            int stride2,
            int width,
            int height);

        static inline bool EqualPlane(const uint8_t* data1,
            const uint8_t* data2,
            int stride,
            int width,
            int height) {
            return EqualPlane(data1, data2, stride, stride, width, height);
        }

        bool FramesEqual(const webrtc::VideoFrame& f1, const webrtc::VideoFrame& f2);

        bool FrameBufsEqual(const rtc::scoped_refptr<webrtc::VideoFrameBuffer>& f1,
            const rtc::scoped_refptr<webrtc::VideoFrameBuffer>& f2);

        rtc::scoped_refptr<I420Buffer> ReadI420Buffer(int width, int height, FILE*);

    }  // namespace test
}  // namespace webrtc


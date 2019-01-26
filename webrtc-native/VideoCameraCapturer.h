#pragma once
#include "TestVideoCapturer.h"

class VideoCameraCapturer : public TestVideoCapturer,
    public rtc::VideoSinkInterface<webrtc::VideoFrame> {
 public:
  static VideoCameraCapturer* Create(size_t width,
                             size_t height,
                             size_t target_fps,
                             size_t capture_device_index);
  virtual ~VideoCameraCapturer();

  void OnFrame(const webrtc::VideoFrame& frame) override;

 private:
  VideoCameraCapturer();
  bool Init(size_t width,
            size_t height,
            size_t target_fps,
            size_t capture_device_index);
  void Destroy();

  rtc::scoped_refptr<webrtc::VideoCaptureModule> vcm_;
  webrtc::VideoCaptureCapability capability_;
};

#pragma once

std::unique_ptr<webrtc::VideoEncoderFactory> CreateEncoderFactory(bool force_software_encoder);

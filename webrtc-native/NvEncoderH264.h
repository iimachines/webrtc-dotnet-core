#pragma once
#include "macros.h"

namespace nvenc {
	class NvEncoder;
}

namespace webrtc {

    class NvEncoderH264 final : public VideoEncoder {
    public:
        static bool IsAvailable();

        explicit NvEncoderH264();
        ~NvEncoderH264() override;

        DISALLOW_COPY_MOVE_ASSIGN(NvEncoderH264);

        // |max_payload_size| is ignored.
        // The following members of |codec_settings| are used. The rest are ignored.
        // - codecType (must be kVideoCodecH264)
        // - targetBitrate
        // - maxFramerate
        // - width
        // - height
        int32_t InitEncode(const VideoCodec* codec_settings,
            int32_t number_of_cores,
            size_t max_payload_size) override;
        int32_t Release() override;

        int32_t RegisterEncodeCompleteCallback(
            EncodedImageCallback* callback) override;
        int32_t SetRateAllocation(const VideoBitrateAllocation& bitrate_allocation,
            uint32_t framerate) override;

        // The result of encoding - an EncodedImage and RTPFragmentationHeader - are
        // passed to the encode complete callback.
        int32_t Encode(const VideoFrame& frame,
            const CodecSpecificInfo* codec_specific_info,
            const std::vector<FrameType>* frame_types) override;

        EncoderInfo GetEncoderInfo() const override;

    private:
        H264BitstreamParser h264_bitstream_parser_;
        
        // Reports statistics with histograms.
        void ReportInit();
        void ReportError();
        void SetStreamState(bool send_stream);

		nvenc::NvEncoder* encoder;
        std::vector<uint8_t> encoded_output_buffer_;
        EncodedImage encoded_image_;

        VideoCodec codec_;
        size_t max_payload_size_;
        EncodedImageCallback* encoded_image_callback_;

        bool has_reported_init_;
        bool has_reported_error_;

        bool is_sending_ = false;
        bool key_frame_request_ = false;

        FILE* debug_output_file = nullptr;
    };

}  // namespace webrtc

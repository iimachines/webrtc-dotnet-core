#include "pch.h"
#include "NativeVideoBuffer.h"
#include "NvEncoderH264.h"
#include "AppEncD3D11.h"

namespace webrtc {

	namespace
	{

		// Used by histograms. Values of entries should not be changed.
		enum H264EncoderImplEvent
		{
			kH264EncoderEventInit = 0,
			kH264EncoderEventError = 1,
			kH264EncoderEventMax = 16,
		};
	}

    NvEncoderH264::NvEncoderH264()
        : max_payload_size_(0)
        , encoded_image_callback_(nullptr)
        , has_reported_init_(false)
        , has_reported_error_(false)
    {
        // debug_output_file = fopen("c:\\temp\\debug.h264", "wb");
    }

    NvEncoderH264::~NvEncoderH264() {
        Release();

        if (debug_output_file)
        {
            fclose(debug_output_file);
            debug_output_file = nullptr;
        }
    }

    int32_t NvEncoderH264::InitEncode(const VideoCodec* codec_settings, int32_t number_of_cores, size_t max_payload_size) {
        ReportInit();
        if (!codec_settings || codec_settings->codecType != kVideoCodecH264) {
            ReportError();
            return WEBRTC_VIDEO_CODEC_ERR_PARAMETER;
        }
        if (codec_settings->maxFramerate == 0) {
            ReportError();
            return WEBRTC_VIDEO_CODEC_ERR_PARAMETER;
        }
        if (codec_settings->width < 1 || codec_settings->height < 1) {
            ReportError();
            return WEBRTC_VIDEO_CODEC_ERR_PARAMETER;
        }

        const int32_t release_ret = Release();
        if (release_ret != WEBRTC_VIDEO_CODEC_OK) {
            ReportError();
            return release_ret;
        }

        const int number_of_streams = SimulcastUtility::NumberOfSimulcastStreams(*codec_settings);
        if (number_of_streams > 1)
        {
            // TODO: Support simulcast
            return WEBRTC_VIDEO_CODEC_ERR_SIMULCAST_PARAMETERS_NOT_SUPPORTED;
        }

        max_payload_size_ = max_payload_size;
        codec_ = *codec_settings;

        // Code expects simulcastStream resolutions to be correct, make sure they are
        // filled even when there are no simulcast layers.
        if (codec_.numberOfSimulcastStreams == 0) {
            codec_.simulcastStream[0].width = codec_.width;
            codec_.simulcastStream[0].height = codec_.height;
        }

        // Temporal layers not supported.
        if (codec_settings->simulcastStream[0].numberOfTemporalLayers > 1) {
            Release();
            return WEBRTC_VIDEO_CODEC_ERR_SIMULCAST_PARAMETERS_NOT_SUPPORTED;
        }

        const auto width = codec_.simulcastStream[0].width;
        const auto height = codec_.simulcastStream[0].height;

        is_sending_ = false;
        key_frame_request_ = false;

		encoder = new nvenc::NvEncoder(width, height);

		// TODO initial configuration of bitrate etc

       /* auto& nvPipe = getNvPipe();

        const auto nvEncoder = nvPipe.CreateEncoder(
            NvPipe::Format::BGRA32,
            NvPipe::Codec::H264,
            NvPipe::Compression::LOSSY,
            codec_.maxBitrate * 1000, 
            codec_.maxFramerate);

        if (!nvEncoder)
        {
            // Failed to create encoder.
            RTC_LOG(LS_ERROR) << "Failed to create NVENC H264 encoder";
            RTC_DCHECK(!nvEncoder);
            Release();
            ReportError();
            return WEBRTC_VIDEO_CODEC_ERROR;
        }

        // Store h264 encoder.
        encoder_ = nvEncoder;*/

        // Create encoded output buffer
        const size_t new_capacity = 4 * width * height;
        encoded_output_buffer_.resize(new_capacity);
        encoded_image_._completeFrame = true;
        encoded_image_._encodedWidth = width;
        encoded_image_._encodedHeight = height;
        encoded_image_.set_buffer(&encoded_output_buffer_[0], new_capacity);
        encoded_image_.set_size(0);

        SimulcastRateAllocator init_allocator(codec_);
        VideoBitrateAllocation allocation = init_allocator.GetAllocation(codec_.startBitrate * 1000, codec_.maxFramerate);
        return SetRateAllocation(allocation, codec_.maxFramerate);
    }

    int32_t NvEncoderH264::Release() {
        if (encoder)
        {
			delete encoder;
			encoder = nullptr;
        }

        encoded_output_buffer_.clear();
        encoded_image_.set_buffer(&encoded_output_buffer_[0], 0);

        is_sending_ = false;
        key_frame_request_ = false;

        return WEBRTC_VIDEO_CODEC_OK;
    }

    int32_t NvEncoderH264::RegisterEncodeCompleteCallback(
        EncodedImageCallback* callback) {
        encoded_image_callback_ = callback;
        return WEBRTC_VIDEO_CODEC_OK;
    }

    int32_t NvEncoderH264::SetRateAllocation(const VideoBitrateAllocation& bitrate, uint32_t new_framerate) {

        if (new_framerate < 1)
            return WEBRTC_VIDEO_CODEC_ERR_PARAMETER;

        if (bitrate.get_sum_bps() == 0) {
            // Encoder paused, turn off all encoding.
            SetStreamState(false);
            return WEBRTC_VIDEO_CODEC_OK;
        }

        // At this point, bitrate allocation should already match codec settings.
        if (codec_.maxBitrate > 0)
            RTC_DCHECK_LE(bitrate.get_sum_kbps(), codec_.maxBitrate);
        RTC_DCHECK_GE(bitrate.get_sum_kbps(), codec_.minBitrate);
        if (codec_.numberOfSimulcastStreams > 0)
            RTC_DCHECK_GE(bitrate.get_sum_kbps(), codec_.simulcastStream[0].minBitrate);

        codec_.maxFramerate = new_framerate;

        const auto target_bps = bitrate.get_sum_bps();

        if (target_bps) {
            // Reconfigure encoder
            SetStreamState(true);

			// TODO set bit rate???
            /*auto& nvPipe = getNvPipe();
            nvPipe.SetBitrate(encoder_, target_bps, new_framerate);*/
        }
        else {
            SetStreamState(false);
        }

        return WEBRTC_VIDEO_CODEC_OK;
    }

    int32_t NvEncoderH264::Encode(const VideoFrame& input_frame,
        const CodecSpecificInfo* codec_specific_info,
        const std::vector<FrameType>* frame_types) {
        /*if (!encoder_) {
            ReportError();
            return WEBRTC_VIDEO_CODEC_UNINITIALIZED;
        }*/
        if (!encoded_image_callback_) {
            RTC_LOG(LS_WARNING)
                << "InitEncode() has been called, but a callback function "
                << "has not been set with RegisterEncodeCompleteCallback()";
            ReportError();
            return WEBRTC_VIDEO_CODEC_UNINITIALIZED;
        }

        const auto frame_buffer = input_frame.video_frame_buffer();
        RTC_CHECK(frame_buffer->type() == VideoFrameBuffer::Type::kNative);
        const auto native_buffer = dynamic_cast<NativeVideoBuffer*>(frame_buffer.get());
        RTC_CHECK(native_buffer != nullptr);

        bool send_key_frame = false;
        if (key_frame_request_ && is_sending_) {
            send_key_frame = true;
        }

        if (!send_key_frame && frame_types && !frame_types->empty()) {
            if ((*frame_types)[0] == kVideoFrameKey && is_sending_) {
                send_key_frame = true;
            }
        }

        const auto width = frame_buffer->width();
        const auto height = frame_buffer->height();

        RTC_DCHECK_EQ(encoded_image_._encodedWidth, width);
        RTC_DCHECK_EQ(encoded_image_._encodedHeight, height);

        if (is_sending_ && (frame_types == nullptr || frame_types->at(0) != kEmptyFrame))
        {
            const auto encoded_buffer_ptr = &encoded_output_buffer_[0];

            // Encode!
            uint64_t encoded_buffer_size = 0;

            switch (native_buffer->format())
            {
            case VideoFrameFormat::GpuTextureD3D11:
            {
                auto* texture = reinterpret_cast<ID3D11Texture2D*>(const_cast<void*>(native_buffer->texture()));

				encoded_buffer_size = encoder->EncodeFrame(texture, encoded_buffer_ptr, encoded_output_buffer_.size());
				break;
            }

            default:
                RTC_LOG(LS_ERROR) << "NVENC H264 encoder does not support format " << static_cast<int>(native_buffer->format());
                return WEBRTC_VIDEO_CODEC_ERROR;
            }

           /* if (encoded_buffer_size == 0)
            {
                RTC_LOG(LS_ERROR) << "NVENC H264 frame encoding failed, EncodeFrame returned " << nvPipe.GetError(encoder_);
                ReportError();
                return WEBRTC_VIDEO_CODEC_ERROR;
            }*/

            if (debug_output_file)
            {
                fwrite(encoded_buffer_ptr, 1, encoded_buffer_size, debug_output_file);
                fflush(debug_output_file);
            }

            encoded_image_.set_size(encoded_buffer_size);
            encoded_image_.qp_ = 5; // TODO: Why was this hardcoded by Microsoft's 3D streaming toolkit? It seems it is replaced anyway by code below (GetLastSliceQp)
            encoded_image_._encodedWidth = width;
            encoded_image_._encodedHeight = height;
            encoded_image_.SetTimestamp(input_frame.timestamp());
            encoded_image_.ntp_time_ms_ = input_frame.ntp_time_ms();
            encoded_image_.capture_time_ms_ = input_frame.render_time_ms();
            encoded_image_.rotation_ = input_frame.rotation();
            encoded_image_.SetColorSpace(input_frame.color_space());
            encoded_image_.content_type_ =
                (codec_.mode == VideoCodecMode::kScreensharing)
                ? VideoContentType::SCREENSHARE
                : VideoContentType::UNSPECIFIED;
            encoded_image_.timing_.flags = VideoSendTiming::kInvalid;
            encoded_image_.SetSpatialIndex(0);

            RTPFragmentationHeader frag_header;

            // This code is copied from the 3D Streaming Toolkit (MIT license)
            // https://github.com/3DStreamingToolkit/3DStreamingToolkit
            {
                std::vector<H264::NaluIndex> NALUidx;
                auto p_nal = encoded_buffer_ptr;
                NALUidx = H264::FindNaluIndices(p_nal, encoded_buffer_size);
                size_t i_nal = NALUidx.size();
                if (i_nal == 0)
                {
                    // TODO: Check this
                    return WEBRTC_VIDEO_CODEC_OK;
                }

                if (i_nal == 1)
                {
                    NALUidx[0].payload_size = encoded_buffer_size - NALUidx[0].payload_start_offset;
                }
                else for (size_t i = 0; i < i_nal; i++)
                {
                    NALUidx[i].payload_size = i + 1 >= i_nal ? encoded_buffer_size - NALUidx[i].payload_start_offset : NALUidx[i + 1].start_offset - NALUidx[i].payload_start_offset;
                }

                frag_header.VerifyAndAllocateFragmentationHeader(i_nal);

                uint32_t totalNaluIndex = 0;
                for (size_t nal_index = 0; nal_index < i_nal; nal_index++)
                {
                    const size_t currentNaluSize = NALUidx[nal_index].payload_size; //i_frame_size
                    frag_header.fragmentationOffset[totalNaluIndex] = NALUidx[nal_index].payload_start_offset;
                    frag_header.fragmentationLength[totalNaluIndex] = currentNaluSize;
                    frag_header.fragmentationPlType[totalNaluIndex] = H264::ParseNaluType(p_nal[NALUidx[nal_index].payload_start_offset]);
                    frag_header.fragmentationTimeDiff[totalNaluIndex] = 0;
                    totalNaluIndex++;
                }
            }

            // Encoder can skip frames to save bandwidth in which case
            // |encoded_images_[i]._length| == 0.
            if (encoded_image_.size() > 0) {
                // Parse QP.
                h264_bitstream_parser_.ParseBitstream(encoded_image_.data(), encoded_image_.size());
                h264_bitstream_parser_.GetLastSliceQp(&encoded_image_.qp_);

                // Deliver encoded image.
                CodecSpecificInfo codec_specific;
                codec_specific.codecType = kVideoCodecH264;
                codec_specific.codecSpecific.H264.packetization_mode = H264PacketizationMode::NonInterleaved;
                encoded_image_callback_->OnEncodedImage(encoded_image_, &codec_specific, &frag_header);
            }
        }
        return WEBRTC_VIDEO_CODEC_OK;
    }

    void NvEncoderH264::ReportInit() {
        if (has_reported_init_)
            return;
        RTC_HISTOGRAM_ENUMERATION("WebRTC.Video.NvEncoderH264.Event",
            kH264EncoderEventInit, kH264EncoderEventMax);
        has_reported_init_ = true;
    }

    void NvEncoderH264::ReportError() {
        if (has_reported_error_)
            return;
        RTC_HISTOGRAM_ENUMERATION("WebRTC.Video.NvEncoderH264.Event",
            kH264EncoderEventError, kH264EncoderEventMax);
        has_reported_error_ = true;
    }

    VideoEncoder::EncoderInfo NvEncoderH264::GetEncoderInfo() const {
        // TODO: Figure out what this "quality scaling" means.
        // TODO: I guess it means we adopt the video resolution of the client?
        EncoderInfo info;
        info.supports_native_handle = true;
        info.implementation_name = "WMP_NVENC_H264";
        info.scaling_settings = ScalingSettings(ScalingSettings::kOff);
        info.is_hardware_accelerated = true;
        info.has_internal_source = false;
        return info;
    }

    void NvEncoderH264::SetStreamState(bool send_stream) {
        if (send_stream && !is_sending_) {
            // Need a key frame if we have not sent this stream before.
            key_frame_request_ = true;
        }
        is_sending_ = send_stream;
    }

    bool NvEncoderH264::IsAvailable()
    {
		// TODO detect presence of nvenc??
        /*static bool is_present = isNvEncoderPresent();
        return is_present;*/
		return true;
    }
}  // namespace webrtc

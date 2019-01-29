#include "pch.h"
#include "NvPipe.h"
#include "NativeVideoBuffer.h"
#include "NvEncoderH264.h"

// TODO: https://stackoverflow.com/questions/33185966/how-to-stream-h-264-video-over-udp-using-the-nvidia-nvenc-hardware-encoder

namespace webrtc {

    namespace {

        // Used by histograms. Values of entries should not be changed.
        enum H264EncoderImplEvent {
            kH264EncoderEventInit = 0,
            kH264EncoderEventError = 1,
            kH264EncoderEventMax = 16,
        };

        bool isNvEncoderPresent()
        {
            // TODO: Find a better way to see if we can create the NvEnc encoder.
            const auto encoder = NvPipe_CreateEncoder(NVPIPE_BGRA32, NVPIPE_H264, NVPIPE_LOSSY, 1000000, 60);
            const bool is_available = encoder != nullptr;
            NvPipe_Destroy(encoder);
            return is_available;
        }
    }  // namespace

    NvEncoderH264::NvEncoderH264()
        : max_payload_size_(0)
        , encoded_image_callback_(nullptr)
        , has_reported_init_(false)
        , has_reported_error_(false) 
    {
        encoded_images_.reserve(kMaxSimulcastStreams);
        encoders_.reserve(kMaxSimulcastStreams);
        configurations_.reserve(kMaxSimulcastStreams);

        debug_output_file = fopen("c:\\temp\\debug.h264", "wb");
    }

    NvEncoderH264::~NvEncoderH264() {
        Release();

        if (debug_output_file)
        {
            fclose(debug_output_file);
            debug_output_file = nullptr;
        }
    }



    int32_t NvEncoderH264::InitEncode(const VideoCodec* inst, int32_t number_of_cores, size_t max_payload_size) {
        ReportInit();
        if (!inst || inst->codecType != kVideoCodecH264) {
            ReportError();
            return WEBRTC_VIDEO_CODEC_ERR_PARAMETER;
        }
        if (inst->maxFramerate == 0) {
            ReportError();
            return WEBRTC_VIDEO_CODEC_ERR_PARAMETER;
        }
        if (inst->width < 1 || inst->height < 1) {
            ReportError();
            return WEBRTC_VIDEO_CODEC_ERR_PARAMETER;
        }

        int32_t release_ret = Release();
        if (release_ret != WEBRTC_VIDEO_CODEC_OK) {
            ReportError();
            return release_ret;
        }

        int number_of_streams = SimulcastUtility::NumberOfSimulcastStreams(*inst);
        if (number_of_streams > 1)
        {
            // TODO: Support simulcast
            return WEBRTC_VIDEO_CODEC_ERR_SIMULCAST_PARAMETERS_NOT_SUPPORTED;
        }

        encoded_images_.resize(number_of_streams);
        encoders_.resize(number_of_streams);
        configurations_.resize(number_of_streams);

        max_payload_size_ = max_payload_size;
        codec_ = *inst;

        // Code expects simulcastStream resolutions to be correct, make sure they are
        // filled even when there are no simulcast layers.
        if (codec_.numberOfSimulcastStreams == 0) {
            codec_.simulcastStream[0].width = codec_.width;
            codec_.simulcastStream[0].height = codec_.height;
        }

        for (int i = 0, idx = number_of_streams - 1; i < number_of_streams; ++i, --idx) {
            assert(i == 0);

            // Temporal layers still not supported.
            if (inst->simulcastStream[i].numberOfTemporalLayers > 1) {
                Release();
                return WEBRTC_VIDEO_CODEC_ERR_SIMULCAST_PARAMETERS_NOT_SUPPORTED;
            }

            const auto width = codec_.simulcastStream[idx].width;
            const auto height = codec_.simulcastStream[idx].height;

            // Set internal settings from codec_settings
            configurations_[i].simulcast_idx = idx;
            configurations_[i].sending = false;
            configurations_[i].width = width;
            configurations_[i].height = height;
            configurations_[i].max_frame_rate = static_cast<float>(codec_.maxFramerate);
            configurations_[i].frame_dropping_on = codec_.H264()->frameDroppingOn;
            configurations_[i].key_frame_interval = codec_.H264()->keyFrameInterval;

            // Codec_settings uses kbits/second; encoder uses bits/second.
            // TODO: Figure out how these settings can be configured.
            configurations_[i].max_bps = 128 * 1000 * 1000; //  width * height * codec_.maxFramerate * 4 * 7 / 100;
            configurations_[i].target_bps = configurations_[i].max_bps / 2;

            auto nvEncoder = NvPipe_CreateEncoder(
                NVPIPE_BGRA32, NVPIPE_H264, NVPIPE_LOSSY,
                configurations_[i].target_bps, configurations_[i].max_frame_rate);

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
            encoders_[i] = nvEncoder;

            // Create encoded output buffer
            const size_t new_capacity = 4 * width * height;
            encoded_output_buffer_.resize(new_capacity);
            //encoded_images_[i].set_buffer(new uint8_t[new_capacity], new_capacity);

            encoded_images_[i]._completeFrame = true;
            encoded_images_[i]._encodedWidth = width;
            encoded_images_[i]._encodedHeight = height;
            encoded_images_[i].set_buffer(&encoded_output_buffer_[0], new_capacity);
            encoded_images_[i].set_size(0);
        }

        SimulcastRateAllocator init_allocator(codec_);
        VideoBitrateAllocation allocation = init_allocator.GetAllocation(codec_.startBitrate * 1000, codec_.maxFramerate);
        return SetRateAllocation(allocation, codec_.maxFramerate);
    }

    int32_t NvEncoderH264::Release() {
        while (!encoders_.empty()) {
            const auto nvEncoder = encoders_.back();
            NvPipe_Destroy(nvEncoder);
            encoders_.pop_back();
        }
        configurations_.clear();
        encoded_images_.clear();
        encoded_output_buffer_.clear();
        return WEBRTC_VIDEO_CODEC_OK;
    }

    int32_t NvEncoderH264::RegisterEncodeCompleteCallback(
        EncodedImageCallback* callback) {
        encoded_image_callback_ = callback;
        return WEBRTC_VIDEO_CODEC_OK;
    }

    int32_t NvEncoderH264::SetRateAllocation(const VideoBitrateAllocation& bitrate, uint32_t new_framerate) {
        if (encoders_.empty())
            return WEBRTC_VIDEO_CODEC_UNINITIALIZED;

        if (new_framerate < 1)
            return WEBRTC_VIDEO_CODEC_ERR_PARAMETER;

        if (bitrate.get_sum_bps() == 0) {
            // Encoder paused, turn off all encoding.
            for (size_t i = 0; i < configurations_.size(); ++i)
                configurations_[i].SetStreamState(false);
            return WEBRTC_VIDEO_CODEC_OK;
        }

        // At this point, bitrate allocation should already match codec settings.
        if (codec_.maxBitrate > 0)
            RTC_DCHECK_LE(bitrate.get_sum_kbps(), codec_.maxBitrate);
        RTC_DCHECK_GE(bitrate.get_sum_kbps(), codec_.minBitrate);
        if (codec_.numberOfSimulcastStreams > 0)
            RTC_DCHECK_GE(bitrate.get_sum_kbps(), codec_.simulcastStream[0].minBitrate);

        codec_.maxFramerate = new_framerate;

        size_t stream_idx = encoders_.size() - 1;
        for (size_t i = 0; i < encoders_.size(); ++i, --stream_idx) {
            // Update layer config.
            // TODO: Figure out how to pass bitrate
            configurations_[i].max_frame_rate = static_cast<float>(new_framerate);
            configurations_[i].max_bps = configurations_[i].width * configurations_[i].height * new_framerate * 4 * 7 / 100;
            configurations_[i].target_bps = configurations_[i].max_bps / 2;

            if (configurations_[i].target_bps) {
                configurations_[i].SetStreamState(true);
                // Reconfigure encoder
                // NvPipe_SetBitrate(encoders_[i], configurations_[i].target_bps, new_framerate);
            }
            else {
                configurations_[i].SetStreamState(false);
            }
        }

        return WEBRTC_VIDEO_CODEC_OK;
    }

    int32_t NvEncoderH264::Encode(const VideoFrame& input_frame,
        const CodecSpecificInfo* codec_specific_info,
        const std::vector<FrameType>* frame_types) {
        if (encoders_.empty()) {
            ReportError();
            return WEBRTC_VIDEO_CODEC_UNINITIALIZED;
        }
        if (!encoded_image_callback_) {
            RTC_LOG(LS_WARNING)
                << "InitEncode() has been called, but a callback function "
                << "has not been set with RegisterEncodeCompleteCallback()";
            ReportError();
            return WEBRTC_VIDEO_CODEC_UNINITIALIZED;
        }

        const auto frame_buffer = input_frame.video_frame_buffer();
        assert(frame_buffer->type() == VideoFrameBuffer::Type::kNative);
        const auto native_buffer = dynamic_cast<NativeVideoBuffer*>(frame_buffer.get());
        assert(native_buffer);

        bool send_key_frame = false;
        for (size_t i = 0; i < configurations_.size(); ++i) {
            if (configurations_[i].key_frame_request && configurations_[i].sending) {
                send_key_frame = true;
                break;
            }
        }
        if (!send_key_frame && frame_types) {
            for (size_t i = 0; i < frame_types->size() && i < configurations_.size();
                ++i) {
                if ((*frame_types)[i] == kVideoFrameKey && configurations_[i].sending) {
                    send_key_frame = true;
                    break;
                }
            }
        }

        const auto width = frame_buffer->width();
        const auto height = frame_buffer->height();

        RTC_DCHECK_EQ(configurations_[0].width, width);
        RTC_DCHECK_EQ(configurations_[0].height, height);

        // Encode image for each layer.
        for (size_t i = 0; i < encoders_.size(); ++i) {
            assert(i == 0);

            auto timeStamp = input_frame.ntp_time_ms();

            if (!configurations_[i].sending) {
                continue;
            }
            if (frame_types != nullptr) {
                // Skip frame?
                if ((*frame_types)[i] == kEmptyFrame) {
                    continue;
                }
            }

            auto encoded_buffer_ptr = &encoded_output_buffer_[0];

            // Encode!
            auto encoded_buffer_size = NvPipe_Encode(encoders_[i],
                native_buffer->texture(), width * 4,
                encoded_buffer_ptr, encoded_output_buffer_.size(),
                width, height, send_key_frame);

            if (encoded_buffer_size == 0)
            {
                RTC_LOG(LS_ERROR) << "NVENC H264 frame encoding failed, EncodeFrame returned " << NvPipe_GetError(encoders_[i]);
                ReportError();
                return WEBRTC_VIDEO_CODEC_ERROR;
            }

            if (debug_output_file)
            {
                fwrite(encoded_buffer_ptr, 1, encoded_buffer_size, debug_output_file);
                fflush(debug_output_file);
            }

            encoded_images_[i].set_size(encoded_buffer_size);
            encoded_images_[i].qp_ = 5;

            encoded_images_[i]._encodedWidth = width;
            encoded_images_[i]._encodedHeight = height;
            encoded_images_[i].SetTimestamp(input_frame.timestamp());
            encoded_images_[i].ntp_time_ms_ = input_frame.ntp_time_ms();
            encoded_images_[i].capture_time_ms_ = input_frame.render_time_ms();
            encoded_images_[i].rotation_ = input_frame.rotation();
            encoded_images_[i].SetColorSpace(input_frame.color_space());
            encoded_images_[i].content_type_ =
                (codec_.mode == VideoCodecMode::kScreensharing)
                ? VideoContentType::SCREENSHARE
                : VideoContentType::UNSPECIFIED;
            encoded_images_[i].timing_.flags = VideoSendTiming::kInvalid;
            encoded_images_[i].SetSpatialIndex(configurations_[i].simulcast_idx);

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
                    size_t currentNaluSize = 0;
                    currentNaluSize = NALUidx[nal_index].payload_size; //i_frame_size

                    frag_header.fragmentationOffset[totalNaluIndex] = NALUidx[nal_index].payload_start_offset;
                    frag_header.fragmentationLength[totalNaluIndex] = currentNaluSize;
                    frag_header.fragmentationPlType[totalNaluIndex] = H264::ParseNaluType(p_nal[NALUidx[nal_index].payload_start_offset]);
                    frag_header.fragmentationTimeDiff[totalNaluIndex] = 0;
                    totalNaluIndex++;
                }
            }

            // Encoder can skip frames to save bandwidth in which case
            // |encoded_images_[i]._length| == 0.
            if (encoded_images_[i].size() > 0) {
                // Parse QP.
                h264_bitstream_parser_.ParseBitstream(encoded_images_[i].data(),
                    encoded_images_[i].size());
                h264_bitstream_parser_.GetLastSliceQp(&encoded_images_[i].qp_);

                // Deliver encoded image.
                CodecSpecificInfo codec_specific;
                codec_specific.codecType = kVideoCodecH264;
                codec_specific.codecSpecific.H264.packetization_mode = H264PacketizationMode::NonInterleaved;
                encoded_image_callback_->OnEncodedImage(encoded_images_[i], &codec_specific, &frag_header);
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

    void NvEncoderH264::LayerConfig::SetStreamState(bool send_stream) {
        if (send_stream && !sending) {
            // Need a key frame if we have not sent this stream before.
            key_frame_request = true;
        }
        sending = send_stream;
    }

    bool NvEncoderH264::IsAvailable()
    {
        static bool is_present = isNvEncoderPresent();
        return is_present;
    }
}  // namespace webrtc

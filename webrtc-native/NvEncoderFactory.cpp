#include "pch.h"
#include "NvEncoderH264.h"
#include "NvEncoderFactory.h"
#include "media/base/h264_profile_level_id.h"

using namespace webrtc;

namespace
{
    bool IsFormatSupported(const std::vector<SdpVideoFormat>& supported_formats,
        const SdpVideoFormat& format)
    {
        for (const SdpVideoFormat& supported_format : supported_formats)
        {
            if (cricket::IsSameCodec(format.name, format.parameters,
                supported_format.name,
                supported_format.parameters))
            {
                return true;
            }
        }
        return false;
    }
}

SdpVideoFormat CreateH264Format(H264::Profile profile, H264::Level level, const std::string& packetization_mode) {
    const absl::optional<std::string> profile_string = H264::ProfileLevelIdToString(H264::ProfileLevelId(profile, level));
    RTC_CHECK(profile_string);
    return SdpVideoFormat(
        cricket::kH264CodecName,
        { {cricket::kH264FmtpProfileLevelId, *profile_string},
         {cricket::kH264FmtpLevelAsymmetryAllowed, "1"},
         {cricket::kH264FmtpPacketizationMode, packetization_mode} });
}

class NvEncoderFactory : public VideoEncoderFactory
{
private:
    std::vector<SdpVideoFormat> supported_formats_;

public:
    NvEncoderFactory()
    {
        // TODO: Figure out supported formats for NVENC
        supported_formats_.push_back(CreateH264Format(H264::kProfileBaseline, H264::kLevel3_1, "1"));
        supported_formats_.push_back(CreateH264Format(H264::kProfileBaseline, H264::kLevel3_1, "0"));
        supported_formats_.push_back(CreateH264Format(H264::kProfileConstrainedBaseline, H264::kLevel3_1, "1"));
        supported_formats_.push_back(CreateH264Format(H264::kProfileConstrainedBaseline, H264::kLevel3_1, "0"));
    }

    CodecInfo QueryVideoEncoder(const SdpVideoFormat& format) const override
    {
        // Format must be one of the internal formats.
        RTC_DCHECK(IsFormatSupported(supported_formats_, format));
     
        CodecInfo info;
        info.has_internal_source = false;
        info.is_hardware_accelerated = false;
        return info;
    }

    std::unique_ptr<VideoEncoder> CreateVideoEncoder(const SdpVideoFormat& format) override
    {
        RTC_DCHECK(IsFormatSupported(supported_formats_, format));
        return std::make_unique<NvEncoderH264>(cricket::VideoCodec(format));
    }

    std::vector<SdpVideoFormat> GetSupportedFormats() const override
    {
        return supported_formats_;
    }
};

std::unique_ptr<VideoEncoderFactory> CreateNvEncoderFactory()
{
    return std::make_unique<NvEncoderFactory>();
}

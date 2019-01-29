#include "pch.h"
#include "EncoderFactory.h"
#include "NvEncoderH264.h"

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
        // TODO: Check capability of NVENC hardware to figure out supported formats.
        // https://en.wikipedia.org/wiki/H.264/MPEG-4_AVC#Levels
        supported_formats_.push_back(CreateH264Format(H264::kProfileBaseline, H264::kLevel5, "0"));
        supported_formats_.push_back(CreateH264Format(H264::kProfileConstrainedBaseline, H264::kLevel5, "0"));
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
        return std::make_unique<NvEncoderH264>();
    }

    std::vector<SdpVideoFormat> GetSupportedFormats() const override
    {
        return supported_formats_;
    }
};

std::unique_ptr<VideoEncoderFactory> CreateEncoderFactory(bool force_software_encoder)
{
    if (!force_software_encoder && NvEncoderH264::IsAvailable())
        return std::make_unique<NvEncoderFactory>();

    // Fallback to VP8 if no licensed NVEnc hardware encoder is found.
    return std::make_unique<InternalEncoderFactory>();
}


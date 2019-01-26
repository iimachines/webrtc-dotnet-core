#pragma once

#pragma warning( push )
#pragma warning( disable : 4244 )

#include <cstdio>
#include <cstdint>

#include <mutex>
#include <map>
#include <memory>
#include <string>
#include <vector>
#include <utility>
#include <limits>

#include "api/scoped_refptr.h"
#include "api/media_stream_interface.h"
#include "api/data_channel_interface.h"
#include "api/peer_connection_interface.h"
#include "api/create_peerconnection_factory.h"
#include "api/video_track_source_proxy.h"

#include "api/video/video_sink_interface.h"
#include "api/video/video_frame.h"
#include "api/video/video_frame_buffer.h"
#include "api/video/video_source_interface.h"
#include "api/video/video_rotation.h"
#include "api/video/i420_buffer.h"
#include "api/video/i010_buffer.h"

#include "api/audio_codecs/builtin_audio_decoder_factory.h"
#include "api/audio_codecs/builtin_audio_encoder_factory.h"

#include "common_types.h"  // NOLINT(build/include)
#include "common_video/include/video_frame_buffer.h"
#include "common_video/libyuv/include/webrtc_libyuv.h"
#include "common_video/h264/h264_bitstream_parser.h"

#include "media/base/video_adapter.h"
#include "media/base/video_broadcaster.h"

#include "media/engine/internal_decoder_factory.h"
#include "media/engine/internal_encoder_factory.h"
#include "media/engine/multiplex_codec_factory.h"

#include "modules/video_capture/video_capture_factory.h"
#include "modules/video_capture/video_capture.h"

#include "modules/video_coding/utility/simulcast_rate_allocator.h"
#include "modules/video_coding/utility/simulcast_utility.h"
#include <modules/video_coding/include/video_error_codes.h>

#include "pc/video_track_source.h"

#include "absl/memory/memory.h"
#include "absl/strings/match.h"

#include "rtc_base/checks.h"
#include "rtc_base/logging.h"
#include "rtc_base/bind.h"
#include "rtc_base/checks.h"
#include "rtc_base/keep_ref_until_done.h"
#include "rtc_base/random.h"
#include "rtc_base/critical_section.h"
#include "rtc_base/task_queue.h"
#include "rtc_base/task_utils/repeating_task.h"
#include "rtc_base/ssl_adapter.h"

#include "system_wrappers/include/clock.h"
#include "system_wrappers/include/metrics.h"

#include "third_party/openh264/src/codec/api/svc/codec_api.h"
#include "third_party/openh264/src/codec/api/svc/codec_app_def.h"
#include "third_party/openh264/src/codec/api/svc/codec_def.h"
#include "third_party/openh264/src/codec/api/svc/codec_ver.h"

#include "third_party/libyuv/include/libyuv/scale.h"


#pragma warning( pop )

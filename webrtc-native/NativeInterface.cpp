#include "pch.h"
#include "PeerConnection.h"
#include "NvEncoderH264.h"
#include "EncoderFactory.h"
#include "NvPipe.h"

#if defined(WEBRTC_WIN)
#   define WEBRTC_PLUGIN_API __declspec(dllexport)
#elif 
#   define WEBRTC_PLUGIN_API __attribute__((visibility("default")))
#endif

namespace
{
    // TODO: Bundle these globals in a PeerConnectionBuilder class!
    bool g_auto_shutdown = true;
    bool g_use_worker_thread = true;
    bool g_use_signaling_thread = true;
    bool g_force_software_encoder = false;

    // For unit testing.
    bool g_use_fake_encoders = false;
    bool g_use_fake_decoders = false;

    LogSink g_log_sink = nullptr;

    rtc::LoggingSeverity g_minimum_logging_severity = rtc::LS_INFO;

    rtc::CriticalSection g_lock;

    webrtc::PeerConnectionFactoryInterface* g_peer_connection_factory = nullptr;

    std::unique_ptr<rtc::Thread> g_worker_thread;
    std::unique_ptr<rtc::Thread> g_signaling_thread;

    void startThread(std::unique_ptr<rtc::Thread>& thread, bool isUsed)
    {
        rtc::CritScope scope(&g_lock);

        assert(!thread);

        if (isUsed)
        {
            thread.reset(new rtc::Thread());
            thread->Start();
        }
        else
        {
            thread.reset(rtc::Thread::Current());
        }
    }

    void stopThread(std::unique_ptr<rtc::Thread>& thread, bool isUsed)
    {
        rtc::CritScope scope(&g_lock);

        if (isUsed)
        {
            if (thread)
            {
                thread->Stop();
                thread = nullptr;
            }
        }
        else
        {
            thread.release();    // NOLINT(bugprone-unused-return-value)
        }
    }

    webrtc::PeerConnectionFactoryInterface* acquireFactory()
    {
        // Create global factory if needed
        rtc::CritScope scope(&g_lock);

        if (g_peer_connection_factory == nullptr)
        {
            startThread(g_signaling_thread, g_use_signaling_thread);
            startThread(g_worker_thread, g_use_worker_thread);

            // TODO: Support fake audio codec factories. Currently we do not support audio at all.
            const auto audio_encoder_factory = webrtc::CreateBuiltinAudioEncoderFactory();
            const auto audio_decoder_factory = webrtc::CreateBuiltinAudioDecoderFactory();

            std::unique_ptr<webrtc::VideoEncoderFactory> video_encoder_factory;
            if (g_use_fake_encoders)
            {
                video_encoder_factory = std::make_unique<webrtc::FakeVideoEncoderFactory>();
            }
            else
            {
                video_encoder_factory = CreateEncoderFactory(g_force_software_encoder);
            }

            // TODO: Add NVDEC hardware decoder
            std::unique_ptr<webrtc::VideoDecoderFactory> video_decoder_factory;
            if (g_use_fake_decoders)
            {
                video_decoder_factory = std::make_unique<webrtc::FakeVideoDecoderFactory>();
            }
            else
            {
                video_decoder_factory = std::make_unique<webrtc::InternalDecoderFactory>();
            }

            const std::nullptr_t default_adm = nullptr;
            const std::nullptr_t audio_mixer = nullptr;
            const std::nullptr_t audio_processing = nullptr;

            auto factory = CreatePeerConnectionFactory(
                g_worker_thread.get(),
                g_worker_thread.get(),
                g_signaling_thread.get(),
                default_adm,
                audio_encoder_factory,
                audio_decoder_factory,
                move(video_encoder_factory),
                move(video_decoder_factory),
                audio_mixer,
                audio_processing);

            g_peer_connection_factory = std::move(factory);
            g_peer_connection_factory->AddRef();
        }
        else if (g_auto_shutdown)
        {
            g_peer_connection_factory->AddRef();
        }

        return g_peer_connection_factory;
    }

    bool releaseFactory(bool should_release = g_auto_shutdown)
    {
        // Release the factory on last destruction if auto-shutdown is enabled
        rtc::CritScope scope(&g_lock);

        if (should_release && g_peer_connection_factory)
        {
            const auto status = g_peer_connection_factory->Release();

            if (status == rtc::RefCountReleaseStatus::kDroppedLastRef)
            {
                g_peer_connection_factory = nullptr;
                stopThread(g_signaling_thread, g_use_signaling_thread);
                stopThread(g_worker_thread, g_use_signaling_thread);
                return true;
            }
        }

        return g_peer_connection_factory == nullptr;
    }

    class ModuleInitializer : public rtc::LogSink
    {
    public:
        ModuleInitializer()
        {
            rtc::LogMessage::SetLogToStderr(false);
            rtc::LogMessage::LogToDebug(rtc::LoggingSeverity::LS_NONE);
            rtc::LogMessage::AddLogToStream(this, rtc::LS_INFO);

#ifdef WEBRTC_WIN
            WSADATA data;
            if (WSAStartup(MAKEWORD(1, 0), &data))
            {
                RTC_LOG(LS_ERROR) << "Failed to initialize Windows sockets! " << WSAGetLastError();
            }
#endif

            if (!rtc::InitializeSSL())
            {
                RTC_LOG(LS_ERROR) << "Failed to initialize SSL!";
            }
        }

        ~ModuleInitializer()
        {
            rtc::LogMessage::RemoveLogToStream(this);

            if (!rtc::CleanupSSL())
            {
                RTC_LOG(LS_ERROR) << "Failed to cleanup SSL!";
            }

#ifdef WEBRTC_WIN
            if (WSACleanup())
            {
                RTC_LOG(LS_ERROR) << "Failed to cleanup Windows sockets! " << WSAGetLastError();
            }
#endif
        }

        void OnLogMessage(const std::string& message, rtc::LoggingSeverity severity) override
        {
            const auto sink = g_log_sink;

            if (sink && severity >= g_minimum_logging_severity)
            {
                sink(message.c_str(), severity);
            }
        }

        void OnLogMessage(const std::string& message) override
        {
            OnLogMessage(message, rtc::LS_INFO);
        }
    };

    void initializeModule()
    {
        static ModuleInitializer init;
    }
}

extern "C"
{
    WEBRTC_PLUGIN_API bool PumpQueuedMessages(int timeoutInMS)
    {
        return rtc::Thread::Current()->ProcessMessages(timeoutInMS);
    }

    WEBRTC_PLUGIN_API bool Configure(
        bool use_signaling_thread,
        bool use_worker_thread,
        bool force_software_video_encoder,
        bool auto_shutdown,
        bool use_fake_encoders,
        bool use_fake_decoders,
        bool log_to_stderr,
        bool log_to_debug,
        LogSink log_sink,
        rtc::LoggingSeverity minimum_logging_severity)
    {
        rtc::CritScope scope(&g_lock);

        initializeModule();

        if (g_worker_thread || g_signaling_thread)
        {
            if (g_use_signaling_thread != use_signaling_thread ||
                g_use_worker_thread != use_worker_thread ||
                g_force_software_encoder != force_software_video_encoder ||
                g_auto_shutdown != auto_shutdown ||
                g_use_fake_decoders != use_fake_decoders ||
                g_use_fake_encoders != use_fake_encoders)
            {
                RTC_LOG(LS_ERROR) << __FUNCTION__ << " must be called once, before creating the first peer connection";
                return false;
            }

            return true;
        }

        g_use_signaling_thread = use_signaling_thread;
        g_use_worker_thread = use_worker_thread;
        g_force_software_encoder = force_software_video_encoder;
        g_auto_shutdown = auto_shutdown;
        g_use_fake_decoders = use_fake_decoders;
        g_use_fake_encoders = use_fake_encoders;

        g_log_sink = log_sink;
        g_minimum_logging_severity = minimum_logging_severity;

        rtc::LogMessage::SetLogToStderr(log_to_stderr);
        rtc::LogMessage::LogToDebug(log_to_debug ? minimum_logging_severity : rtc::LS_NONE);

        return true;
    }

    WEBRTC_PLUGIN_API bool CanEncodeHardwareTextures()
    {
        return webrtc::NvEncoderH264::IsAvailable();
    }

    WEBRTC_PLUGIN_API bool HasFactory()
    {
        rtc::CritScope scope(&g_lock);
        return g_peer_connection_factory;
    }

    WEBRTC_PLUGIN_API bool Shutdown()
    {
        return releaseFactory(true);
    }

    WEBRTC_PLUGIN_API PeerConnection* CreatePeerConnection(
        const char** turn_url_array,
        const int turn_url_count,
        const char** stun_url_array,
        const int stun_url_count,
        const char* username,
        const char* credential,
        bool can_receive_audio,
        bool can_receive_video,
        bool is_dtls_srtp_enabled)
    {
        rtc::CritScope scope(&g_lock);

        initializeModule();

        auto factory = acquireFactory();
        if (!factory)
            return nullptr;

        auto connection = new PeerConnection(factory,
            turn_url_array, turn_url_count,
            stun_url_array, stun_url_count,
            username, credential,
            can_receive_audio, can_receive_video,
            is_dtls_srtp_enabled);

        if (!connection->created())
        {
            delete connection;
            releaseFactory();
            connection = nullptr;
        }

        return connection;
    }

    WEBRTC_PLUGIN_API void ClosePeerConnection(PeerConnection* connection)
    {
        if (connection)
        {
            delete connection;
            releaseFactory();
        }
    }

    WEBRTC_PLUGIN_API int AddVideoTrack(PeerConnection* connection, const char* label, int min_bps, int max_bps, int max_fps)
    {
        return connection->AddVideoTrack(label, min_bps, max_bps, max_fps);
    }

    WEBRTC_PLUGIN_API bool AddDataChannel(PeerConnection* connection, const char* label, bool is_ordered, bool is_reliable)
    {
        return connection->AddDataChannel(label, is_ordered, is_reliable);
    }

    WEBRTC_PLUGIN_API bool RemoveDataChannel(PeerConnection* connection, const char* label)
    {
        return connection->RemoveDataChannel(label);
    }

    WEBRTC_PLUGIN_API bool CreateOffer(PeerConnection* connection)
    {
        return connection->CreateOffer();
    }

    WEBRTC_PLUGIN_API bool CreateAnswer(PeerConnection* connection)
    {
        return connection->CreateAnswer();
    }

    WEBRTC_PLUGIN_API bool SendData(PeerConnection* connection, const char* label, const uint8_t* data, int length, bool is_binary)
    {
        return connection->SendData(label, data, length, is_binary);
    }

    WEBRTC_PLUGIN_API bool SendVideoFrame(PeerConnection* connection, int trackId, const uint8_t* pixels, int stride, int width, int height, VideoFrameFormat format)
    {
        return connection->SendVideoFrame(trackId, pixels, stride, width, height, format);
    }

    WEBRTC_PLUGIN_API bool SetAudioControl(PeerConnection* connection, bool is_mute, bool is_record)
    {
        return connection->SetAudioControl(is_mute, is_record);
    }

    WEBRTC_PLUGIN_API bool SetRemoteDescription(PeerConnection* connection, const char* type, const char* sdp)
    {
        return connection->SetRemoteDescription(type, sdp);
    }

    WEBRTC_PLUGIN_API bool AddIceCandidate(PeerConnection* connection, const char* candidate, const int sdp_mlineindex,
        const char* sdp_mid)
    {
        return connection->AddIceCandidate(candidate, sdp_mlineindex, sdp_mid);
    }

    // Register callback functions.
    WEBRTC_PLUGIN_API bool RegisterOnLocalI420FrameReady(PeerConnection* connection, I420FrameReadyCallback callback)
    {
        connection->RegisterOnLocalI420FrameReady(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterOnRemoteI420FrameReady(PeerConnection* connection, I420FrameReadyCallback callback)
    {
        connection->RegisterOnRemoteI420FrameReady(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterOnLocalDataChannelReady(PeerConnection* connection, LocalDataChannelReadyCallback callback)
    {
        connection->RegisterOnLocalDataChannelReady(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterOnDataFromDataChannelReady(PeerConnection* connection, DataAvailableCallback callback)
    {
        connection->RegisterOnDataFromDataChannelReady(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterOnFailure(PeerConnection* connection, FailureCallback callback)
    {
        connection->RegisterOnFailure(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterOnAudioBusReady(PeerConnection* connection, AudioBusReadyCallback callback)
    {
        connection->RegisterOnAudioBusReady(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterOnLocalSdpReadyToSend(PeerConnection* connection, LocalSdpReadyToSendCallback callback)
    {
        connection->RegisterOnLocalSdpReadyToSend(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterOnIceCandidateReadyToSend(PeerConnection* connection, IceCandidateReadyToSendCallback callback)
    {
        connection->RegisterOnIceCandidateReadyToSend(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterSignalingStateChanged(PeerConnection* connection, StateChangedCallback callback)
    {
        connection->RegisterSignalingStateChanged(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterConnectionStateChanged(PeerConnection* connection, StateChangedCallback callback)
    {
        connection->RegisterConnectionStateChanged(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterVideoFrameEncoded(PeerConnection* connection, VideoFrameCallback callback)
    {
        connection->RegisterVideoFrameEncoded(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterRemoteTrackChanged(PeerConnection* connection, RemoteTrackChangedCallback  callback)
    {
        connection->RegisterRemoteTrackChanged(callback);
        return true;
    }

    WEBRTC_PLUGIN_API int64_t GetRealtimeClockTimeInMicroseconds()
    {
        const auto clock = webrtc::Clock::GetRealTimeClock();
        return clock->TimeInMicroseconds();
    }
}

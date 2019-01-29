#include "pch.h"
#include "SimplePeerConnection.h"
#include "NvEncoderH264.h"

#if defined(WEBRTC_WIN)
#   define WEBRTC_PLUGIN_API __declspec(dllexport)
#elif 
#   define WEBRTC_PLUGIN_API __attribute__((visibility("default")))
#endif

namespace
{
    class ModuleInitializer
    {
    public:
        ModuleInitializer()
        {
            // TODO: Allow configuration of error level!
#ifdef NDEBUG
            rtc::LogMessage::LogToDebug(rtc::LoggingSeverity::LS_ERROR);
#else
            rtc::LogMessage::LogToDebug(rtc::LoggingSeverity::LS_WARNING);
#endif

            rtc::LogMessage::SetLogToStderr(false);

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

    WEBRTC_PLUGIN_API bool InitializeThreading(bool useSignalingThread, bool useWorkerThread)
    {
        initializeModule();
        return SimplePeerConnection::InitializeThreading(useSignalingThread, useWorkerThread);
    }

    WEBRTC_PLUGIN_API SimplePeerConnection* CreatePeerConnection(
        const char** turn_url_array,
        const int turn_url_count,
        const char** stun_url_array,
        const int stun_url_count,
        const char* username,
        const char* credential,
        bool can_receive_audio,
        bool can_receive_video,
        bool isDtlsSrtpEnabled)
    {
        initializeModule();

        auto connection = new SimplePeerConnection();
        if (connection->InitializePeerConnection(
            turn_url_array, turn_url_count,
            stun_url_array, stun_url_count,
            username, credential,
            can_receive_audio, can_receive_video,
            isDtlsSrtpEnabled))
        {
            return connection;
        }

        delete connection;
        return nullptr;
    }

    WEBRTC_PLUGIN_API void ClosePeerConnection(SimplePeerConnection* connection)
    {
        delete connection;
    }

    WEBRTC_PLUGIN_API bool AddStream(SimplePeerConnection* connection, bool audio, bool video)
    {
        return connection->AddStreams(audio, video);
    }

    WEBRTC_PLUGIN_API bool AddDataChannel(SimplePeerConnection* connection, const char* label, bool is_ordered, bool is_reliable)
    {
        return connection->CreateDataChannel(label, is_ordered, is_reliable);
    }

    WEBRTC_PLUGIN_API bool CreateOffer(SimplePeerConnection* connection)
    {
        return connection->CreateOffer();
    }

    WEBRTC_PLUGIN_API bool CreateAnswer(SimplePeerConnection* connection)
    {
        return connection->CreateAnswer();
    }

    WEBRTC_PLUGIN_API bool SendData(SimplePeerConnection* connection, const char* label, const char* data)
    {
        return connection->SendData(label, data);
    }
    
    WEBRTC_PLUGIN_API bool SendVideoFrame(SimplePeerConnection* connection, const uint8_t* pixels, int stride, int width, int height, VideoFrameFormat format)
    {
        return connection->SendVideoFrame(pixels, stride, width, height, format);
    }

    WEBRTC_PLUGIN_API bool SetAudioControl(SimplePeerConnection* connection, bool is_mute, bool is_record)
    {
        return connection->SetAudioControl(is_mute, is_record);
    }

    WEBRTC_PLUGIN_API bool SetRemoteDescription(SimplePeerConnection* connection, const char* type, const char* sdp)
    {
        return connection->SetRemoteDescription(type, sdp);
    }

    WEBRTC_PLUGIN_API bool AddIceCandidate(SimplePeerConnection* connection, const char* candidate, const int sdp_mlineindex,
        const char* sdp_mid)
    {
        return connection->AddIceCandidate(candidate, sdp_mlineindex, sdp_mid);
    }

    // Register callback functions.
    WEBRTC_PLUGIN_API bool RegisterOnLocalI420FrameReady(SimplePeerConnection* connection, I420FrameReadyCallback callback)
    {
        connection->RegisterOnLocalI420FrameReady(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterOnRemoteI420FrameReady(SimplePeerConnection* connection, I420FrameReadyCallback callback)
    {
        connection->RegisterOnRemoteI420FrameReady(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterOnLocalDataChannelReady(SimplePeerConnection* connection, LocalDataChannelReadyCallback callback)
    {
        connection->RegisterOnLocalDataChannelReady(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterOnDataFromDataChannelReady(SimplePeerConnection* connection, DataAvailableCallback callback)
    {
        connection->RegisterOnDataFromDataChannelReady(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterOnFailure(SimplePeerConnection* connection, FailureCallback callback)
    {
        connection->RegisterOnFailure(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterOnAudioBusReady(SimplePeerConnection* connection, AudioBusReadyCallback callback)
    {
        connection->RegisterOnAudioBusReady(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterOnLocalSdpReadyToSend(SimplePeerConnection* connection, LocalSdpReadyToSendCallback callback)
    {
        connection->RegisterOnLocalSdpReadyToSend(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterOnIceCandidateReadyToSend(SimplePeerConnection* connection, IceCandidateReadyToSendCallback callback)
    {
        connection->RegisterOnIceCandidateReadyToSend(callback);
        return true;
    }

    WEBRTC_PLUGIN_API bool RegisterSignalingStateChanged(SimplePeerConnection* connection, SignalingStateChangedCallback callback)
    {
        connection->RegisterSignalingStateChanged(callback);
        return true;
    }

    WEBRTC_PLUGIN_API int64_t GetRealtimeClockTimeInMicroseconds()
    {
        const auto clock = webrtc::Clock::GetRealTimeClock();
        return clock->TimeInMicroseconds();
    }

    WEBRTC_PLUGIN_API bool CanEncodeHardwareTextures()
    {
        return webrtc::NvEncoderH264::IsAvailable();
    }
}

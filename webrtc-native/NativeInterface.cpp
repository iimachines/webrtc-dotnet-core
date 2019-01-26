#include "pch.h"
#include "SimplePeerConnection.h"

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

static void initializeModule()
{
    static ModuleInitializer init;
}

/* TODO: We might be able to use variadic templates to generate an interface, but my C++ knowledge is too low */

#if 0
template <typename... TArg>
std::function<bool(SimplePeerConnection*, TArg...)> Stub(bool (SimplePeerConnection::*method)(TArg...))
{
    return [](SimplePeerConnection* self, TArg&&... args)
    {
        return std::invoke(self, method, forward<TArg>(args)...);
    };
}

auto AddStream()
{
    return Stub(&SimplePeerConnection::AddStreams);
}
#endif

bool PumpQueuedMessages(int timeoutInMS)
{
    return rtc::Thread::Current()->ProcessMessages(timeoutInMS);
}

bool InitializeThreading(bool useSignalingThread, bool useWorkerThread)
{
    initializeModule();
    return SimplePeerConnection::InitializeThreading(useSignalingThread, useWorkerThread);
}

SimplePeerConnection* CreatePeerConnection(
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

void ClosePeerConnection(SimplePeerConnection* connection)
{
    delete connection;
}

bool AddStream(SimplePeerConnection* connection, bool audio_only)
{
    return connection->AddStreams(audio_only);
}

bool AddDataChannel(SimplePeerConnection* connection, const char* label, bool is_ordered, bool is_reliable)
{
    return connection->CreateDataChannel(label, is_ordered, is_reliable);
}

bool CreateOffer(SimplePeerConnection* connection)
{
    return connection->CreateOffer();
}

bool CreateAnswer(SimplePeerConnection* connection)
{
    return connection->CreateAnswer();
}

bool SendData(SimplePeerConnection* connection, const char* label, const char* data)
{
    return connection->SendData(label, data);
}

bool SetAudioControl(SimplePeerConnection* connection, bool is_mute, bool is_record)
{
    return connection->SetAudioControl(is_mute, is_record);
}

bool SetRemoteDescription(SimplePeerConnection* connection, const char* type, const char* sdp)
{
    return connection->SetRemoteDescription(type, sdp);
}

bool AddIceCandidate(SimplePeerConnection* connection, const char* candidate, const int sdp_mlineindex,
                     const char* sdp_mid)
{
    return connection->AddIceCandidate(candidate, sdp_mlineindex, sdp_mid);
}

// Register callback functions.
bool RegisterOnLocalI420FrameReady(SimplePeerConnection* connection, I420FRAMEREADY_CALLBACK callback)
{
    connection->RegisterOnLocalI420FrameReady(callback);
    return true;
}

bool RegisterOnRemoteI420FrameReady(SimplePeerConnection* connection, I420FRAMEREADY_CALLBACK callback)
{
    connection->RegisterOnRemoteI420FrameReady(callback);
    return true;
}

bool RegisterOnLocalDataChannelReady(SimplePeerConnection* connection, LOCALDATACHANNELREADY_CALLBACK callback)
{
    connection->RegisterOnLocalDataChannelReady(callback);
    return true;
}

bool RegisterOnDataFromDataChannelReady(SimplePeerConnection* connection, DATAFROMEDATECHANNELREADY_CALLBACK callback)
{
    connection->RegisterOnDataFromDataChannelReady(callback);
    return true;
}

bool RegisterOnFailure(SimplePeerConnection* connection, FAILURE_CALLBACK callback)
{
    connection->RegisterOnFailure(callback);
    return true;
}

bool RegisterOnAudioBusReady(SimplePeerConnection* connection, AUDIOBUSREADY_CALLBACK callback)
{
    connection->RegisterOnAudioBusReady(callback);
    return true;
}

// Singnaling channel related functions.
bool RegisterOnLocalSdpReadyToSend(SimplePeerConnection* connection, LOCALSDPREADYTOSEND_CALLBACK callback)
{
    connection->RegisterOnLocalSdpReadyToSend(callback);
    return true;
}

bool RegisterOnIceCandidateReadyToSend(SimplePeerConnection* connection, ICECANDIDATEREADYTOSEND_CALLBACK callback)
{
    connection->RegisterOnIceCandidateReadyToSend(callback);
    return true;
}

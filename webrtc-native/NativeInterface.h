#pragma once

// Definitions of callback functions.
typedef void (*I420FRAMEREADY_CALLBACK)(
    const uint8_t* data_y,
    const uint8_t* data_u,
    const uint8_t* data_v,
    const uint8_t* data_a,
    int stride_y,
    int stride_u,
    int stride_v,
    int stride_a,
    uint32_t width,
    uint32_t height,
    uint64_t timeStampUS);

typedef void (*LOCALDATACHANNELREADY_CALLBACK)(const char* label);

typedef void (*DATAFROMEDATECHANNELREADY_CALLBACK)(const char* label, const char* msg);

typedef void (*FAILURE_CALLBACK)(const char* msg);

typedef void (*LOCALSDPREADYTOSEND_CALLBACK)(const char* type, const char* sdp);

typedef void (*ICECANDIDATEREADYTOSEND_CALLBACK)(const char* candidate,
                                                 const int sdp_mline_index,
                                                 const char* sdp_mid);

typedef void (*AUDIOBUSREADY_CALLBACK)(const void* audio_data,
                                       int bits_per_sample,
                                       int sample_rate,
                                       int number_of_channels,
                                       int number_of_frames);

#if defined(WEBRTC_WIN)
#define WEBRTC_PLUGIN_API __declspec(dllexport)
#elif defined(WEBRTC_ANDROID)
#define WEBRTC_PLUGIN_API __attribute__((visibility("default")))
#endif

class SimplePeerConnection;

extern "C" {
WEBRTC_PLUGIN_API SimplePeerConnection* CreatePeerConnection(
    const char** turn_url_array,
    const int turn_url_count,
    const char** stun_url_array,
    const int stun_url_count,
    const char* username,
    const char* credential,
    bool can_receive_audio,
    bool can_receive_video,
    bool isDtlsSrtpEnabled);

// Pump webrtc messages, needed when running in a single thread, for testing/debugging
// Returns false when the thread is quit.
WEBRTC_PLUGIN_API bool PumpQueuedMessages(int timeoutInMS);

// Initializes the threading model used. Must be called before creating the first connection. Defaults to creating both signaling and worker threads
WEBRTC_PLUGIN_API bool InitializeThreading(bool useSignalingThread, bool useWorkerThread);

// Close a peerconnection.
WEBRTC_PLUGIN_API void ClosePeerConnection(SimplePeerConnection* connection);

// Add a audio stream. If audio_only is true, the stream only has an audio track and no video track.
WEBRTC_PLUGIN_API bool AddStream(SimplePeerConnection* connection, bool audio_only);

// Add a data channel to peer connection.
WEBRTC_PLUGIN_API bool AddDataChannel(SimplePeerConnection* connection, const char* label, bool is_ordered,
                                      bool is_reliable);

// Create a peer connection offer.
WEBRTC_PLUGIN_API bool CreateOffer(SimplePeerConnection* connection);

// Create a peer connection answer.
WEBRTC_PLUGIN_API bool CreateAnswer(SimplePeerConnection* connection);

// Send data through data channel.
WEBRTC_PLUGIN_API bool SendData(SimplePeerConnection* connection, const char* label, const char* data);

// Set audio control. If is_mute=true, no audio will playout. 
// If is_record=true, AUDIOBUSREADY_CALLBACK will be called every 10 ms.
WEBRTC_PLUGIN_API bool SetAudioControl(SimplePeerConnection* connection, bool is_mute, bool is_record);

// Set remote sdp.
WEBRTC_PLUGIN_API bool SetRemoteDescription(SimplePeerConnection* connection, const char* type, const char* sdp);

// Add ice candidate.
WEBRTC_PLUGIN_API bool AddIceCandidate(SimplePeerConnection* connection, const char* candidate,
                                       const int sdp_mlineindex, const char* sdp_mid);

// Register callback functions.
WEBRTC_PLUGIN_API bool
RegisterOnLocalI420FrameReady(SimplePeerConnection* connection, I420FRAMEREADY_CALLBACK callback);
WEBRTC_PLUGIN_API bool RegisterOnRemoteI420FrameReady(SimplePeerConnection* connection,
                                                      I420FRAMEREADY_CALLBACK callback);
WEBRTC_PLUGIN_API bool RegisterOnLocalDataChannelReady(SimplePeerConnection* connection,
                                                       LOCALDATACHANNELREADY_CALLBACK callback);
WEBRTC_PLUGIN_API bool RegisterOnDataFromDataChannelReady(SimplePeerConnection* connection,
                                                          DATAFROMEDATECHANNELREADY_CALLBACK callback);
WEBRTC_PLUGIN_API bool RegisterOnFailure(SimplePeerConnection* connection, FAILURE_CALLBACK callback);
WEBRTC_PLUGIN_API bool RegisterOnAudioBusReady(SimplePeerConnection* connection, AUDIOBUSREADY_CALLBACK callback);
WEBRTC_PLUGIN_API bool RegisterOnLocalSdpReadyToSend(SimplePeerConnection* connection,
                                                     LOCALSDPREADYTOSEND_CALLBACK callback);
WEBRTC_PLUGIN_API bool RegisterOnIceCandidateReadyToSend(SimplePeerConnection* connection,
                                                         ICECANDIDATEREADYTOSEND_CALLBACK callback);
}

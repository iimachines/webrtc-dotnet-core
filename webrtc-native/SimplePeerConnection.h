#pragma once

#include "NativeInterface.h"
#include "VideoObserver.h"
#include "InjectableVideoTrackSource.h"

#undef HAS_LOCAL_VIDEO_OBSERVER

class SimplePeerConnection final
    : public webrtc::PeerConnectionObserver
    , public webrtc::CreateSessionDescriptionObserver
    , public webrtc::AudioTrackSinkInterface
{
public:
    SimplePeerConnection();
    ~SimplePeerConnection() override;

    bool InitializePeerConnection(const char** turn_url_array,
        const int turn_url_count,
        const char** stun_url_array,
        const int stun_url_count,
        const char* username,
        const char* credential, bool can_receive_audio, bool canReceiveVideo, bool enable_dtls_srtp);

    // TODO: Allow the user to select what stream!
    bool AddStreams(bool audio, bool video);
    bool CreateOffer();
    bool CreateAnswer();
    bool SetAudioControl(bool is_mute, bool is_record);

    bool CreateDataChannel(const char* label, bool is_ordered, bool is_reliable);
    bool SendData(const char* label, const std::string& data);

    bool SendVideoFrame(const uint8_t* pixels, int stride, int width, int height, PixelFormat format) const;

    // Register callback functions.
    void RegisterOnLocalI420FrameReady(I420FrameReadyCallback callback) const;
    void RegisterOnRemoteI420FrameReady(I420FrameReadyCallback callback) const;
    void RegisterOnLocalDataChannelReady(LocalDataChannelReadyCallback callback);
    void RegisterOnDataFromDataChannelReady(DataAvailableCallback callback);
    void RegisterOnFailure(FailureCallback callback);
    void RegisterOnAudioBusReady(AudioBusReadyCallback callback);
    void RegisterOnLocalSdpReadyToSend(LocalSdpReadyToSendCallback callback);
    void RegisterOnIceCandidateReadyToSend(IceCandidateReadyToSendCallback callback);
    void RegisterSignalingStateChanged(SignalingStateChangedCallback callback);

    bool SetRemoteDescription(const char* type, const char* sdp) const;
    bool AddIceCandidate(const char* sdp,
        const int sdp_mlineindex,
        const char* sdp_mid) const;

    void AddRef() const override;
    rtc::RefCountReleaseStatus Release() const override;

    static bool InitializeThreading(bool use_signaling_thread, bool use_worker_thread);

protected:
    // create a peer connection and add the turn servers info to the configuration.
    bool CreatePeerConnection(
        const char** turn_url_array, const int turn_url_count,
        const char** stun_url_array, const int stun_url_count,
        const char* username, const char* credential,
        bool enable_dtls_srtp);

    void CloseDataChannel(const char* name);

    bool SetAudioControl();

    // PeerConnectionObserver implementation.
    void OnSignalingChange(webrtc::PeerConnectionInterface::SignalingState new_state) override;

    void OnAddStream(rtc::scoped_refptr<webrtc::MediaStreamInterface> stream) override;

    void OnRemoveStream(rtc::scoped_refptr<webrtc::MediaStreamInterface> stream) override
    {
    }

    void OnDataChannel(rtc::scoped_refptr<webrtc::DataChannelInterface> channel) override;

    void OnRenegotiationNeeded() override
    {
    }

    void OnIceConnectionChange(webrtc::PeerConnectionInterface::IceConnectionState new_state) override
    {
    }

    void OnIceGatheringChange(webrtc::PeerConnectionInterface::IceGatheringState new_state) override
    {
    }

    void OnIceCandidate(const webrtc::IceCandidateInterface* candidate) override;

    void OnIceConnectionReceivingChange(bool receiving) override
    {
    }

    // CreateSessionDescriptionObserver implementation.
    void OnSuccess(webrtc::SessionDescriptionInterface* desc) override;
    void OnFailure(webrtc::RTCError error) override;

    // AudioTrackSinkInterface implementation.
    void OnData(const void* audio_data,
        int bits_per_sample,
        int sample_rate,
        size_t number_of_channels,
        size_t number_of_frames) override;

    // Get remote audio tracks ssrcs.
    std::vector<uint32_t> GetRemoteAudioTrackSynchronizationSources() const;

private:
    rtc::scoped_refptr<webrtc::PeerConnectionInterface> peer_connection_;

    class DataChannelEntry : public webrtc::DataChannelObserver
    {
    public:
        DataChannelEntry(SimplePeerConnection* connection,
            rtc::scoped_refptr<webrtc::DataChannelInterface> data_channel);
        ~DataChannelEntry() override;

        // DataChannelObserver implementation.
        void OnStateChange() override;
        void OnMessage(const webrtc::DataBuffer& buffer) override;

        rtc::scoped_refptr<webrtc::DataChannelInterface> channel;
        SimplePeerConnection* connection;

    private:
        // disallow copy-and-assign
        DataChannelEntry(const DataChannelEntry&) = delete;
        DataChannelEntry& operator=(const DataChannelEntry&) = delete;
    };

    std::map<std::string, std::unique_ptr<DataChannelEntry>> data_channels_;
    std::map<std::string, rtc::scoped_refptr<webrtc::MediaStreamInterface>> active_streams_;

    rtc::scoped_refptr<webrtc::InjectableVideoTrackSource> video_track_source_;

#ifdef HAS_LOCAL_VIDEO_OBSERVER
    std::unique_ptr<VideoObserver> local_video_observer_;
#endif

    std::unique_ptr<VideoObserver> remote_video_observer_;

    rtc::scoped_refptr<webrtc::MediaStreamInterface> remote_stream_;

    webrtc::PeerConnectionInterface::RTCConfiguration config_;

    LocalDataChannelReadyCallback OnLocalDataChannelReady = nullptr;
    DataAvailableCallback OnDataFromDataChannelReady = nullptr;
    FailureCallback OnFailureMessage = nullptr;
    AudioBusReadyCallback OnAudioReady = nullptr;

    LocalSdpReadyToSendCallback OnLocalSdpReadyToSend = nullptr;
    IceCandidateReadyToSendCallback OnIceCandidateReady = nullptr;
    SignalingStateChangedCallback OnSignalingStateChanged = nullptr;

    bool is_mute_audio_ = false;
    bool is_record_audio_ = false;
    bool can_receive_audio_ = false;
    bool can_receive_video_ = false;

    // disallow copy-and-assign
    SimplePeerConnection(const SimplePeerConnection&) = delete;
    SimplePeerConnection& operator=(const SimplePeerConnection&) = delete;
};

#pragma once

#include "NativeInterface.h"
#include "VideoObserver.h"

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

    bool AddStreams(bool audio_only);
    bool CreateOffer();
    bool CreateAnswer();
    bool SetAudioControl(bool is_mute, bool is_record);

    bool CreateDataChannel(const char* label, bool is_ordered, bool is_reliable);
    bool SendData(const char* label, const std::string& data);

    // Register callback functions.
    void RegisterOnLocalI420FrameReady(I420FRAMEREADY_CALLBACK callback) const;
    void RegisterOnRemoteI420FrameReady(I420FRAMEREADY_CALLBACK callback) const;
    void RegisterOnLocalDataChannelReady(LOCALDATACHANNELREADY_CALLBACK callback);
    void RegisterOnDataFromDataChannelReady(
        DATAFROMEDATECHANNELREADY_CALLBACK callback);
    void RegisterOnFailure(FAILURE_CALLBACK callback);
    void RegisterOnAudioBusReady(AUDIOBUSREADY_CALLBACK callback);
    void RegisterOnLocalSdpReadyToSend(LOCALSDPREADYTOSEND_CALLBACK callback);
    void RegisterOnIceCandidateReadyToSend(
        ICECANDIDATEREADYTOSEND_CALLBACK callback);
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
    void OnSignalingChange(webrtc::PeerConnectionInterface::SignalingState new_state) override
    {
    }

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

    std::unique_ptr<VideoObserver> local_video_observer_;
    std::unique_ptr<VideoObserver> remote_video_observer_;

    webrtc::MediaStreamInterface* remote_stream_ = nullptr;
    webrtc::PeerConnectionInterface::RTCConfiguration config_;

    LOCALDATACHANNELREADY_CALLBACK OnLocalDataChannelReady = nullptr;
    DATAFROMEDATECHANNELREADY_CALLBACK OnDataFromDataChannelReady = nullptr;
    FAILURE_CALLBACK OnFailureMessage = nullptr;
    AUDIOBUSREADY_CALLBACK OnAudioReady = nullptr;

    LOCALSDPREADYTOSEND_CALLBACK OnLocalSdpReadyToSend = nullptr;
    ICECANDIDATEREADYTOSEND_CALLBACK OnIceCandidateReady = nullptr;

    bool is_mute_audio_ = false;
    bool is_record_audio_ = false;
    bool can_receive_audio_ = false;
    bool can_receive_video_ = false;

    // disallow copy-and-assign
    SimplePeerConnection(const SimplePeerConnection&) = delete;
    SimplePeerConnection& operator=(const SimplePeerConnection&) = delete;
};

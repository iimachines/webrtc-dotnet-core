#pragma once

#include "NativeInterface.h"
#include "VideoObserver.h"
#include "VideoFrameEvents.h"

#undef HAS_LOCAL_VIDEO_OBSERVER
#define HAS_REMOTE_VIDEO_OBSERVER

class PeerConnection final
    : public webrtc::PeerConnectionObserver
    , public webrtc::CreateSessionDescriptionObserver
    , public webrtc::AudioTrackSinkInterface
    , public VideoFrameEvents
{
public:
    PeerConnection(
        webrtc::PeerConnectionFactoryInterface* factory,
        const char** ice_url_array, const int ice_url_count,
        const char* ice_username, const char* ice_password, 
        bool can_receive_audio, bool can_receive_video, 
        bool enable_dtls_srtp);

    ~PeerConnection() override;

    bool created() const { return peer_connection_; }

    rtc::scoped_refptr<webrtc::PeerConnectionFactoryInterface>& factory() { return factory_; }

    // TODO: Allow the user to select the kind of stream (what camera, etc...)
    int AddVideoTrack(const std::string& label, int min_bps, int max_bps, int max_fps);
    bool SetBitrate(const webrtc::BitrateSettings& bitrate);
    bool SendVideoFrame(int video_track_id, const uint8_t* pixels, int stride, int width, int height, VideoFrameFormat format);

    bool CreateOffer();
    bool CreateAnswer();
    bool SetAudioControl(bool is_mute, bool is_record);

    bool AddDataChannel(const char* label, bool is_ordered, bool is_reliable);
    bool SendData(const char* label, const uint8_t* data, int length, bool is_binary);
    bool RemoveDataChannel(const char* label);


    // Register callback functions.
    void RegisterOnLocalI420FrameReady(IncomingVideoFrameCallback callback) const;
    void RegisterOnRemoteI420FrameReady(IncomingVideoFrameCallback callback) const;
    void RegisterOnLocalDataChannelReady(LocalDataChannelReadyCallback callback);
    void RegisterOnDataFromDataChannelReady(DataAvailableCallback callback);
    void RegisterOnFailure(FailureCallback callback);
    void RegisterOnAudioBusReady(AudioBusReadyCallback callback);
    void RegisterOnLocalSdpReadyToSend(LocalSdpReadyToSendCallback callback);
    void RegisterOnIceCandidateReadyToSend(IceCandidateReadyToSendCallback callback);
    void RegisterSignalingStateChanged(StateChangedCallback callback);
    void RegisterConnectionStateChanged(StateChangedCallback callback);
    void RegisterVideoFrameProcessed(VideoFrameProcessedCallback callback);
    void RegisterRemoteTrackChanged(RemoteTrackChangedCallback callback);

    bool SetRemoteDescription(const char* type, const char* sdp) const;
    bool AddIceCandidate(const char* sdp, const int sdp_mlineindex, const char* sdp_mid) const;

    void AddRef() const override;
    rtc::RefCountReleaseStatus Release() const override;

protected:
    bool CreateTransceivers() const;
    bool SetAudioControl();

    // PeerConnectionObserver implementation.
    void OnSignalingChange(webrtc::PeerConnectionInterface::SignalingState new_state) override;

    void OnConnectionChange(webrtc::PeerConnectionInterface::PeerConnectionState new_state) override;


    void OnTrack(rtc::scoped_refptr<webrtc::RtpTransceiverInterface> transceiver) override;

    void OnRenegotiationNeeded() override;

    void OnIceConnectionChange(webrtc::PeerConnectionInterface::IceConnectionState new_state) override;

    void OnIceGatheringChange(webrtc::PeerConnectionInterface::IceGatheringState new_state) override;

    void OnDataChannel(rtc::scoped_refptr<webrtc::DataChannelInterface> channel) override;

    void OnIceCandidate(const webrtc::IceCandidateInterface* candidate) override;

    // CreateSessionDescriptionObserver implementation.
    void OnSuccess(webrtc::SessionDescriptionInterface* desc) override;

    void OnFailure(webrtc::RTCError error) override;

    // AudioTrackSinkInterface implementation.
    void OnData(const void* audio_data,
        int bits_per_sample,
        int sample_rate,
        size_t number_of_channels,
        size_t number_of_frames) override;

    void OnFrameProcessed(int video_track_id, const void* pixels, bool is_encoded) override;

    // Get remote audio tracks ssrcs.
    std::vector<uint32_t> GetRemoteAudioTrackSynchronizationSources() const;

private:
    rtc::scoped_refptr<webrtc::PeerConnectionFactoryInterface> factory_;
    rtc::scoped_refptr<webrtc::PeerConnectionInterface> peer_connection_;

    class DataChannelEntry : public webrtc::DataChannelObserver
    {
    public:
        DataChannelEntry(PeerConnection* connection,
            rtc::scoped_refptr<webrtc::DataChannelInterface> data_channel);
        ~DataChannelEntry() override;

        // DataChannelObserver implementation.
        void OnStateChange() override;
        void OnMessage(const webrtc::DataBuffer& buffer) override;

        rtc::scoped_refptr<webrtc::DataChannelInterface> channel;
        PeerConnection* connection;

    private:
        // disallow copy-and-assign
        DataChannelEntry(const DataChannelEntry&) = delete;
        DataChannelEntry& operator=(const DataChannelEntry&) = delete;
    };

    int last_video_track_id_ = 0;

    // TODO: Also use an identifier of a data-channel.
    std::map<std::string, std::unique_ptr<DataChannelEntry>> data_channels_;

    std::map<int, rtc::scoped_refptr<webrtc::VideoTrackInterface>> video_tracks_;

#ifdef HAS_LOCAL_VIDEO_OBSERVER
    std::unique_ptr<VideoObserver> local_video_observer_;
#endif

#ifdef HAS_REMOTE_VIDEO_OBSERVER
    std::unique_ptr<VideoObserver> remote_video_observer_;
#endif

    // rtc::scoped_refptr<webrtc::MediaStreamInterface> remote_stream_;

    webrtc::PeerConnectionInterface::RTCConfiguration config_;

    LocalDataChannelReadyCallback OnLocalDataChannelReady = nullptr;
    DataAvailableCallback OnDataFromDataChannelReady = nullptr;
    FailureCallback OnFailureMessage = nullptr;
    AudioBusReadyCallback OnAudioReady = nullptr;
    VideoFrameProcessedCallback OnVideoFrameProcessed = nullptr;

    LocalSdpReadyToSendCallback OnLocalSdpReadyToSend = nullptr;
    IceCandidateReadyToSendCallback OnIceCandidateReady = nullptr;
    StateChangedCallback OnSignalingStateChanged = nullptr;
    StateChangedCallback OnConnectionStateChanged = nullptr;
    RemoteTrackChangedCallback OnRemoteTrackChanged = nullptr;

    bool is_mute_audio_ = false;
    bool is_record_audio_ = false;
    bool can_receive_audio_ = false;
    bool can_receive_video_ = false;

    // disallow copy-and-assign
    PeerConnection(const PeerConnection&) = delete;
    PeerConnection& operator=(const PeerConnection&) = delete;
};

#include "pch.h"

#include "PeerConnection.h"
#include "InjectableVideoTrackSource.h"
#include "DummySetSessionDescriptionObserver.h"
#include "EncoderFactory.h"
#include "NativeVideoBuffer.h"

namespace
{
    // TODO: This should not be part of our peer connection file!
    int g_peer_count = 0;

    bool g_use_worker_thread = true;
    bool g_use_signaling_thread = true;
    bool g_force_software_encoder = false;

    std::unique_ptr<rtc::Thread> g_worker_thread;
    std::unique_ptr<rtc::Thread> g_signaling_thread;

    // TODO: Give each peer connection its own factory, with different configuration?
    rtc::scoped_refptr<webrtc::PeerConnectionFactoryInterface> g_peer_connection_factory;

    void startThread(std::unique_ptr<rtc::Thread>& thread, bool isUsed)
    {
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
        if (isUsed)
        {
            thread.reset();
        }
        else
        {
            thread.release();    // NOLINT(bugprone-unused-return-value)
        }
    }

    auto getYuvConverter(VideoFrameFormat pf)
    {
        switch (pf)
        {
        case VideoFrameFormat::BGRA32: return libyuv::ARGBToI420;
        case VideoFrameFormat::RGBA32: return libyuv::ABGRToI420;
        case VideoFrameFormat::ARGB32: return libyuv::BGRAToI420;
        case VideoFrameFormat::ABGR32: return libyuv::RGBAToI420;
        default:
            throw std::runtime_error("No YUV converter for pixel format " + std::to_string(static_cast<int>(pf)));
        }
    }

    std::string GetEnvVarOrDefault(const char* env_var_name, const char* default_value)
    {
        std::string value;
        const char* env_var = getenv(env_var_name);
        if (env_var)
            value = env_var;

        if (value.empty())
            value = default_value;

        return value;
    }
} // namespace

PeerConnection::PeerConnection()
{
    g_peer_count++;
}

PeerConnection::~PeerConnection()
{
    g_peer_count--;

    // Destruct all data channels.
    data_channels_.clear();

    peer_connection_ = nullptr;

    if (g_peer_count == 0)
    {
        g_peer_connection_factory = nullptr;
        stopThread(g_signaling_thread, g_use_signaling_thread);
        stopThread(g_worker_thread, g_use_signaling_thread);
    }
}

bool PeerConnection::InitializePeerConnection(
    const char** turn_url_array,
    const int turn_url_count,
    const char** stun_url_array,
    const int stun_url_count,
    const char* username,
    const char* credential,
    bool can_receive_audio,
    bool can_receive_video,
    bool enable_dtls_srtp)
{
    RTC_DCHECK(peer_connection_.get() == nullptr);

    if (g_peer_connection_factory == nullptr)
    {
        startThread(g_signaling_thread, g_use_signaling_thread);
        startThread(g_worker_thread, g_use_worker_thread);

        const auto audioEncoderFactory = webrtc::CreateBuiltinAudioEncoderFactory();
        const auto audioDecoderFactory = webrtc::CreateBuiltinAudioDecoderFactory();
        auto videoEncoderFactory = CreateEncoderFactory(g_force_software_encoder);
        auto videoDecoderFactory = std::make_unique<webrtc::InternalDecoderFactory>();

        const std::nullptr_t default_adm = nullptr;
        const std::nullptr_t audio_mixer = nullptr;
        const std::nullptr_t audio_processing = nullptr;

        g_peer_connection_factory = webrtc::CreatePeerConnectionFactory(
            g_worker_thread.get(),
            g_worker_thread.get(),
            g_signaling_thread.get(),
            default_adm,
            audioEncoderFactory,
            audioDecoderFactory,
            move(videoEncoderFactory),
            move(videoDecoderFactory),
            audio_mixer,
            audio_processing);
    }

    if (!g_peer_connection_factory)
    {
        return false;
    }

    if (!CreatePeerConnection(
        turn_url_array, turn_url_count,
        stun_url_array, stun_url_count,
        username, credential,
        enable_dtls_srtp))
    {
        return false;
    }

    can_receive_audio_ = can_receive_audio;
    can_receive_video_ = can_receive_video;

    return peer_connection_;
}

bool PeerConnection::CreatePeerConnection(
    const char** turn_url_array,
    const int turn_url_count,
    const char** stun_url_array,
    const int stun_url_count,
    const char* username,
    const char* credential,
    bool enable_dtls_srtp)
{
    RTC_DCHECK(g_peer_connection_factory.get() != nullptr);
    RTC_DCHECK(peer_connection_.get() == nullptr);

#ifdef HAS_LOCAL_VIDEO_OBSERVER
    local_video_observer_.reset(new VideoObserver());
#endif

#ifdef HAS_REMOTE_VIDEO_OBSERVER
    remote_video_observer_.reset(new VideoObserver());
#endif

    // Add the turn server.
    if (turn_url_array != nullptr && turn_url_count > 0)
    {
        webrtc::PeerConnectionInterface::IceServer turn_server;
        for (int i = 0; i < turn_url_count; i++)
        {
            std::string url(turn_url_array[i]);
            if (url.length() > 0)
                turn_server.urls.emplace_back(url);
        }

        std::string user_name(username);
        if (user_name.length() > 0)
            turn_server.username = username;

        std::string password(credential);
        if (password.length() > 0)
            turn_server.password = credential;

        config_.servers.push_back(turn_server);
    }

    // Add the stun server.
    if (stun_url_array != nullptr && stun_url_count > 0)
    {
        webrtc::PeerConnectionInterface::IceServer stun_server;
        for (int i = 0; i < turn_url_count; i++)
        {
            std::string url(turn_url_array[i]);
            if (url.length() > 0)
                stun_server.urls.emplace_back(url);
        }
        config_.servers.push_back(stun_server);
    }

    // TODO: Allow passing in this configuration
    config_.tcp_candidate_policy = webrtc::PeerConnectionInterface::kTcpCandidatePolicyDisabled;
    config_.disable_ipv6 = true;
    config_.enable_dtls_srtp = enable_dtls_srtp;
    config_.rtcp_mux_policy = webrtc::PeerConnectionInterface::kRtcpMuxPolicyRequire;
    config_.sdp_semantics = webrtc::SdpSemantics::kUnifiedPlan;

    peer_connection_ = g_peer_connection_factory->CreatePeerConnection(
        config_, nullptr, nullptr, this);

    return peer_connection_;
}

bool PeerConnection::CreateOffer()
{
    if (!peer_connection_.get())
        return false;

    webrtc::PeerConnectionInterface::RTCOfferAnswerOptions options;
    options.offer_to_receive_audio = can_receive_audio_;
    options.offer_to_receive_video = can_receive_video_;
    peer_connection_->CreateOffer(this, options);
    return true;
}

bool PeerConnection::CreateAnswer()
{
    if (!peer_connection_.get())
        return false;

    webrtc::PeerConnectionInterface::RTCOfferAnswerOptions options;
    options.offer_to_receive_audio = can_receive_audio_;
    options.offer_to_receive_video = can_receive_video_;
    peer_connection_->CreateAnswer(this, options);
    return true;
}

void PeerConnection::OnSuccess(
    webrtc::SessionDescriptionInterface* desc)
{
    peer_connection_->SetLocalDescription(
        DummySetSessionDescriptionObserver::Create(), desc);

    std::string sdp;
    desc->ToString(&sdp);

    if (OnLocalSdpReadyToSend)
        OnLocalSdpReadyToSend(desc->type().c_str(), sdp.c_str());
}

void PeerConnection::OnFailure(webrtc::RTCError error)
{
    RTC_LOG(LERROR) << ToString(error.type()) << ": " << error.message();

    // TODO(hta): include error.type in the message
    if (OnFailureMessage)
        OnFailureMessage(error.message());
}

void PeerConnection::OnIceCandidate(
    const webrtc::IceCandidateInterface* candidate)
{
    RTC_LOG(INFO) << __FUNCTION__ << " " << candidate->sdp_mline_index();

    std::string sdp;
    if (!candidate->ToString(&sdp))
    {
        RTC_LOG(LS_ERROR) << "Failed to serialize candidate";
        return;
    }

    if (OnIceCandidateReady)
        OnIceCandidateReady(sdp.c_str(), candidate->sdp_mline_index(),
            candidate->sdp_mid().c_str());
}

void PeerConnection::RegisterOnLocalI420FrameReady(I420FrameReadyCallback callback) const
{
#ifdef HAS_LOCAL_VIDEO_OBSERVER
    if (local_video_observer_)
        local_video_observer_->SetVideoCallback(callback);
#endif
}

void PeerConnection::RegisterOnRemoteI420FrameReady(I420FrameReadyCallback callback) const
{
#ifdef HAS_REMOTE_VIDEO_OBSERVER
    if (remote_video_observer_)
        remote_video_observer_->SetVideoCallback(callback);
#endif
}

void PeerConnection::RegisterOnLocalDataChannelReady(LocalDataChannelReadyCallback callback)
{
    OnLocalDataChannelReady = callback;
}

void PeerConnection::RegisterOnDataFromDataChannelReady(DataAvailableCallback callback)
{
    OnDataFromDataChannelReady = callback;
}

void PeerConnection::RegisterOnFailure(FailureCallback callback)
{
    OnFailureMessage = callback;
}

void PeerConnection::RegisterOnAudioBusReady(AudioBusReadyCallback callback)
{
    OnAudioReady = callback;
}

void PeerConnection::RegisterOnLocalSdpReadyToSend(LocalSdpReadyToSendCallback callback)
{
    OnLocalSdpReadyToSend = callback;
}

void PeerConnection::RegisterOnIceCandidateReadyToSend(IceCandidateReadyToSendCallback callback)
{
    OnIceCandidateReady = callback;
}

void PeerConnection::RegisterSignalingStateChanged(SignalingStateChangedCallback callback)
{
    OnSignalingStateChanged = callback;
}

bool PeerConnection::SetRemoteDescription(const char* type, const char* sdp) const
{
    if (!peer_connection_)
        return false;

    const std::string remote_desc(sdp);
    const std::string sdp_type(type);
    webrtc::SdpParseError error;
    webrtc::SessionDescriptionInterface* session_description(
        webrtc::CreateSessionDescription(sdp_type, remote_desc, &error));

    if (!session_description)
    {
        RTC_LOG(WARNING) << "Can't parse received session description message. "
            << "SdpParseError was: " << error.description;
        return false;
    }

    RTC_LOG(INFO) << " Received session description :" << remote_desc;
    peer_connection_->SetRemoteDescription(DummySetSessionDescriptionObserver::Create(), session_description);

    return true;
}

bool PeerConnection::AddIceCandidate(const char* candidate, const int sdp_mlineindex, const char* sdp_mid) const
{
    if (!peer_connection_)
        return false;

    webrtc::SdpParseError error;
    std::unique_ptr<webrtc::IceCandidateInterface> ice_candidate(
        CreateIceCandidate(sdp_mid, sdp_mlineindex, candidate, &error));
    if (!ice_candidate)
    {
        RTC_LOG(WARNING) << "Can't parse received candidate message. "
            << "SdpParseError was: " << error.description;
        return false;
    }
    if (!peer_connection_->AddIceCandidate(ice_candidate.get()))
    {
        RTC_LOG(WARNING) << "Failed to apply the received candidate";
        return false;
    }
    RTC_LOG(INFO) << " Received candidate :" << candidate;
    return true;
}

bool PeerConnection::SetAudioControl(bool is_mute, bool is_record)
{
    is_mute_audio_ = is_mute;
    is_record_audio_ = is_record;
    return SetAudioControl();
}

bool PeerConnection::SetAudioControl()
{
    return false;

    // TODO: Implement!
#if 0
    if (!remote_stream_)
        return false;

    webrtc::AudioTrackVector tracks = remote_stream_->GetAudioTracks();
    if (tracks.empty())
        return false;

    webrtc::AudioTrackInterface* audio_track = tracks[0];
    std::string id = audio_track->id();
    if (is_record_audio_)
        audio_track->AddSink(this);
    else
        audio_track->RemoveSink(this);

    for (auto& track : tracks)
    {
        if (is_mute_audio_)
            track->set_enabled(false);
        else
            track->set_enabled(true);
    }

    return true;
#endif
}

void PeerConnection::OnSignalingChange(webrtc::PeerConnectionInterface::SignalingState new_state)
{
    RTC_LOG(INFO) << __FUNCTION__ << " " << new_state;

    if (OnSignalingStateChanged)
        OnSignalingStateChanged(new_state);
}

void PeerConnection::OnAddTrack(
    rtc::scoped_refptr<webrtc::RtpReceiverInterface> receiver, 
    const std::vector<rtc::scoped_refptr<webrtc::MediaStreamInterface>>& streams)
{
    RTC_LOG(INFO) << __FUNCTION__ << " " << receiver->id();

#ifdef HAS_REMOTE_VIDEO_OBSERVER
    if (remote_video_observer_)
    {
        auto track = receiver->track();
        auto video_track = dynamic_cast<webrtc::VideoTrackInterface*>(track.get());

        if (video_track)
        {
            video_track->AddOrUpdateSink(remote_video_observer_.get(), rtc::VideoSinkWants());
        }
    }

    SetAudioControl();
#endif

}

void PeerConnection::OnRemoveTrack(rtc::scoped_refptr<webrtc::RtpReceiverInterface> receiver)
{
    RTC_LOG(INFO) << __FUNCTION__ << " " << receiver->id();
}

void PeerConnection::OnRenegotiationNeeded()
{
    RTC_LOG(INFO) << __FUNCTION__;
    // TODO: Implement
}

void PeerConnection::OnIceConnectionChange(webrtc::PeerConnectionInterface::IceConnectionState new_state)
{
    RTC_LOG(INFO) << __FUNCTION__ << new_state;
    // TODO: Implement
}

void PeerConnection::OnIceGatheringChange(webrtc::PeerConnectionInterface::IceGatheringState new_state)
{
    RTC_LOG(INFO) << __FUNCTION__ << new_state;
    // TODO: Implement
}

int PeerConnection::AddVideoTrack(const std::string& label, int min_bps, int max_bps, int max_fps)
{
    for (auto&& pair : video_tracks_)
    {
        if (pair.second->id() == label)
        {
            RTC_LOG(LS_ERROR) << "Video track '" << label << "' already exists!";
            return 0;
        }
    }

    // TODO: Support audio
    //if (audio)
    //{
    //    const rtc::scoped_refptr<webrtc::AudioTrackInterface> audio_track(
    //        g_peer_connection_factory->CreateAudioTrack(
    //            kAudioLabel, g_peer_connection_factory->CreateAudioSource(
    //                cricket::AudioOptions())));
    //    peer_connection_->AddTrack(audio_track, { kStreamId });
    //}

    auto video_track_source = new rtc::RefCountedObject<webrtc::InjectableVideoTrackSource>();
    if (!video_track_source)
        return 0;

    auto video_track = g_peer_connection_factory->CreateVideoTrack(label, video_track_source);
    if (!video_track)
        return 0;

    webrtc::RtpEncodingParameters init_encoding;
    init_encoding.max_bitrate_bps = max_bps;
    init_encoding.max_framerate = max_fps;
    init_encoding.min_bitrate_bps = max_bps;
    init_encoding.active = true;

    webrtc::RtpTransceiverInit init_params;
    init_params.send_encodings = { init_encoding };

    auto video_transceiver_result = peer_connection_->AddTransceiver(video_track, init_params);
    if (!video_transceiver_result.ok())
        return 0;

    const auto id = ++last_video_track_id_;
    video_tracks_.emplace(id, video_track);
    return id;
}

bool PeerConnection::CreateDataChannel(const char* label, bool is_ordered, bool is_reliable)
{
    struct webrtc::DataChannelInit init;
    init.ordered = is_ordered;
    init.reliable = is_reliable;

    if (data_channels_.count(label))
    {
        RTC_LOG(LS_ERROR) << "Data channel '" << label << "' already exists!";
        return false;
    }

    auto data_channel = peer_connection_->CreateDataChannel(label, &init);
    if (data_channel)
    {
        data_channels_.emplace(label, std::make_unique<DataChannelEntry>(this, data_channel));
        RTC_LOG(LS_INFO) << "Data channel '" << label << "' added";
        return true;
    }

    RTC_LOG(LS_ERROR) << "Failed to create data channel '" << label << "'!";
    return false;
}

void PeerConnection::CloseDataChannel(const char* name)
{
    const auto it = data_channels_.find(name);
    if (it == data_channels_.end())
    {
        RTC_LOG(LS_ERROR) << "Data channel '" << name << "' not found";
        return;
    }

    data_channels_.erase(it);
}

bool PeerConnection::SendData(const char* label, const std::string& data)
{
    const auto it = data_channels_.find(label);
    if (it == data_channels_.end())
    {
        RTC_LOG(LS_ERROR) << "Data channel '" << label << "' not found";
        return false;
    }

    const webrtc::DataBuffer buffer(data);
    return it->second->channel->Send(buffer);
}

bool PeerConnection::SendVideoFrame(int id, const uint8_t* pixels, int stride, int width, int height, VideoFrameFormat format) const
{
    auto it = video_tracks_.find(id);
    if (it == video_tracks_.end())
    {
        RTC_LOG(LS_ERROR) << "Video track #" << id << " not found";
        return false;
    }

    auto source = dynamic_cast<rtc::VideoSinkInterface<webrtc::VideoFrame>*>(it->second->GetSource());
    if (!source)
    {
        RTC_LOG(LS_ERROR) << "Video track #" << id << " does not support sending frames";
        return false;
    }

    //auto senders = peer_connection_->GetSenders();
    //if (!senders.empty())
    //{
    //    for (auto& sender : senders)
    //    {
    //        auto params = sender->GetParameters();
    //        for (auto& encoding : params.encodings)
    //        {
    //            printf("%d %d %d\n", encoding.min_bitrate_bps.value_or(0), encoding.max_bitrate_bps.value_or(0), encoding.max_framerate.value_or(0));
    //        }
    //    }
    //}

    rtc::scoped_refptr<webrtc::VideoFrameBuffer> buffer;

    const auto clock = webrtc::Clock::GetRealTimeClock();

    if (format == VideoFrameFormat::CpuTexture)
    {
        buffer = new rtc::RefCountedObject<webrtc::NativeVideoBuffer>(
            width, height, static_cast<const void*>(pixels));
    }
    else
    {
        auto yuvBuffer = webrtc::I420Buffer::Create(width, height);

        const auto convertToYUV = getYuvConverter(format);

        convertToYUV(pixels, stride,
            yuvBuffer->MutableDataY(), yuvBuffer->StrideY(),
            yuvBuffer->MutableDataU(), yuvBuffer->StrideU(),
            yuvBuffer->MutableDataV(), yuvBuffer->StrideV(),
            width,
            height);

        buffer = yuvBuffer;
    }

    const auto yuvFrame = webrtc::VideoFrame::Builder()
        .set_video_frame_buffer(buffer)
        .set_rotation(webrtc::kVideoRotation_0)
        .set_timestamp_us(clock->TimeInMicroseconds())
        .build();

    source->OnFrame(yuvFrame);
    return true;
}

void PeerConnection::OnDataChannel(rtc::scoped_refptr<webrtc::DataChannelInterface> channel)
{
    const auto label = channel->label();

    if (data_channels_.count(label))
    {
        RTC_LOG(LS_ERROR) << "Data channel '" << label << "' already exists!";
        return;
    }

    data_channels_.emplace(label, std::make_unique<DataChannelEntry>(this, channel));
}

// AudioTrackSinkInterface implementation.
void PeerConnection::OnData(const void* audio_data,
    int bits_per_sample,
    int sample_rate,
    size_t number_of_channels,
    size_t number_of_frames)
{
    if (OnAudioReady)
        OnAudioReady(audio_data, bits_per_sample, sample_rate,
            static_cast<int>(number_of_channels),
            static_cast<int>(number_of_frames));
}

std::vector<uint32_t> PeerConnection::GetRemoteAudioTrackSynchronizationSources() const
{
    std::vector<rtc::scoped_refptr<webrtc::RtpReceiverInterface>> receivers =
        peer_connection_->GetReceivers();

    std::vector<uint32_t> synchronizationSources;
    for (const auto& receiver : receivers)
    {
        if (receiver->media_type() != cricket::MEDIA_TYPE_AUDIO)
            continue;

        std::vector<webrtc::RtpEncodingParameters> params =
            receiver->GetParameters().encodings;

        for (const auto& param : params)
        {
            uint32_t synchronizationSource = param.ssrc.value_or(0);
            if (synchronizationSource > 0)
                synchronizationSources.push_back(synchronizationSource);
        }
    }

    return synchronizationSources;
}

PeerConnection::DataChannelEntry::DataChannelEntry(
    PeerConnection* connection,
    rtc::scoped_refptr<webrtc::DataChannelInterface> data_channel)
    : channel(std::move(std::move(data_channel)))
    , connection(connection)
{
    channel->RegisterObserver(this);
}

PeerConnection::DataChannelEntry::~DataChannelEntry()
{
    if (channel)
    {
        channel->UnregisterObserver();
        channel->Close();
        channel = nullptr;
    }
}

void PeerConnection::DataChannelEntry::OnStateChange()
{
    if (channel)
    {
        const webrtc::DataChannelInterface::DataState state = channel->state();
        if (state == webrtc::DataChannelInterface::kOpen)
        {
            if (connection->OnLocalDataChannelReady)
                connection->OnLocalDataChannelReady(channel->label().c_str());
            RTC_LOG(LS_INFO) << "Data channel is open";
        }
    }
}

//  A data buffer was successfully received.
void PeerConnection::DataChannelEntry::OnMessage(const webrtc::DataBuffer& buffer)
{
    const size_t size = buffer.data.size();
    const bool is_large = size >= 1024;
    char* msg = is_large ? new char[size + 1] : static_cast<char*>(_alloca(size + 1));
    memcpy(msg, buffer.data.data(), size);
    msg[size] = 0;
    if (connection->OnDataFromDataChannelReady)
        connection->OnDataFromDataChannelReady(channel->label().c_str(), msg);
    if (is_large)
    {
        delete[] msg;
    }
}

void PeerConnection::AddRef() const
{
    // Managed by the .NET code, so no-op
}

rtc::RefCountReleaseStatus PeerConnection::Release() const
{
    // Managed by the .NET code, so other refs always remain until disposed or garbage-collected.
    return rtc::RefCountReleaseStatus::kOtherRefsRemained;
}

bool PeerConnection::Configure(bool use_signaling_thread, bool use_worker_thread, bool force_software_video_encoder)
{
    if (g_worker_thread || g_signaling_thread)
    {
        if (g_use_signaling_thread != use_signaling_thread ||
            g_use_worker_thread != use_worker_thread ||
            g_force_software_encoder != force_software_video_encoder)
        {
            RTC_LOG(LS_ERROR) << __FUNCTION__ << " must be called once, before creating the first peer connection";
            return false;
        }

        return true;
    }

    g_use_signaling_thread = use_signaling_thread;
    g_use_worker_thread = use_worker_thread;
    g_force_software_encoder = force_software_video_encoder;
    return true;
}

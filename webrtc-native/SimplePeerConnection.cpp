#include "pch.h"

#include "SimplePeerConnection.h"
#include "CapturerTrackSource.h"
#include "DummySetSessionDescriptionObserver.h"
#include "FrameGeneratorCapturer.h"
#include "NvEncoderFactory.h"

// Names used for media stream ids.
// TODO: Implement getUserMedia like in the web api!
const char kAudioLabel[] = "audio_label";
const char kVideoLabel[] = "video_label";
const char kStreamId[] = "stream_id";

namespace
{
    // TODO: This should not be part of our peer connection class!
    int g_peer_count = 0;

    bool g_use_worker_thread = true;
    bool g_use_signaling_thread = true;

    std::unique_ptr<rtc::Thread> g_worker_thread;
    std::unique_ptr<rtc::Thread> g_signaling_thread;

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

SimplePeerConnection::SimplePeerConnection()
{
    g_peer_count++;
}

SimplePeerConnection::~SimplePeerConnection()
{
    g_peer_count--;

    // Destruct all data channels.
    data_channels_.clear();

    peer_connection_ = nullptr;
    active_streams_.clear();

    if (g_peer_count == 0)
    {
        g_peer_connection_factory = nullptr;
        stopThread(g_signaling_thread, g_use_signaling_thread);
        stopThread(g_worker_thread, g_use_signaling_thread);
    }
}

bool SimplePeerConnection::InitializePeerConnection(
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
        //auto videoEncoderFactory = std::make_unique<webrtc::InternalEncoderFactory>();
        auto videoEncoderFactory = CreateNvEncoderFactory();
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

bool SimplePeerConnection::CreatePeerConnection(
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

    local_video_observer_.reset(new VideoObserver());
    remote_video_observer_.reset(new VideoObserver());

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
    webrtc::PeerConnectionInterface::RTCConfiguration config;
    config.tcp_candidate_policy = webrtc::PeerConnectionInterface::kTcpCandidatePolicyDisabled;
    config.disable_ipv6 = true;
    config.enable_dtls_srtp = enable_dtls_srtp;
    config.rtcp_mux_policy = webrtc::PeerConnectionInterface::kRtcpMuxPolicyRequire;

    peer_connection_ = g_peer_connection_factory->CreatePeerConnection(
        config_, nullptr, nullptr, this);

    return peer_connection_.get() != nullptr;
}

bool SimplePeerConnection::CreateOffer()
{
    if (!peer_connection_.get())
        return false;

    webrtc::PeerConnectionInterface::RTCOfferAnswerOptions options;
    options.offer_to_receive_audio = can_receive_audio_;
    options.offer_to_receive_video = can_receive_video_;
    peer_connection_->CreateOffer(this, options);
    return true;
}

bool SimplePeerConnection::CreateAnswer()
{
    if (!peer_connection_.get())
        return false;

    webrtc::PeerConnectionInterface::RTCOfferAnswerOptions options;
    options.offer_to_receive_audio = can_receive_audio_;
    options.offer_to_receive_video = can_receive_video_;
    peer_connection_->CreateAnswer(this, options);
    return true;
}

void SimplePeerConnection::OnSuccess(
    webrtc::SessionDescriptionInterface* desc)
{
    peer_connection_->SetLocalDescription(
        DummySetSessionDescriptionObserver::Create(), desc);

    std::string sdp;
    desc->ToString(&sdp);

    if (OnLocalSdpReadyToSend)
        OnLocalSdpReadyToSend(desc->type().c_str(), sdp.c_str());
}

void SimplePeerConnection::OnFailure(webrtc::RTCError error)
{
    RTC_LOG(LERROR) << ToString(error.type()) << ": " << error.message();

    // TODO(hta): include error.type in the message
    if (OnFailureMessage)
        OnFailureMessage(error.message());
}

void SimplePeerConnection::OnIceCandidate(
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

void SimplePeerConnection::RegisterOnLocalI420FrameReady(
    I420FRAMEREADY_CALLBACK callback) const
{
    if (local_video_observer_)
        local_video_observer_->SetVideoCallback(callback);
}

void SimplePeerConnection::RegisterOnRemoteI420FrameReady(
    I420FRAMEREADY_CALLBACK callback) const
{
    if (remote_video_observer_)
        remote_video_observer_->SetVideoCallback(callback);
}

void SimplePeerConnection::RegisterOnLocalDataChannelReady(
    LOCALDATACHANNELREADY_CALLBACK callback)
{
    OnLocalDataChannelReady = callback;
}

void SimplePeerConnection::RegisterOnDataFromDataChannelReady(
    DATAFROMEDATECHANNELREADY_CALLBACK callback)
{
    OnDataFromDataChannelReady = callback;
}

void SimplePeerConnection::RegisterOnFailure(FAILURE_CALLBACK callback)
{
    OnFailureMessage = callback;
}

void SimplePeerConnection::RegisterOnAudioBusReady(
    AUDIOBUSREADY_CALLBACK callback)
{
    OnAudioReady = callback;
}

void SimplePeerConnection::RegisterOnLocalSdpReadyToSend(
    LOCALSDPREADYTOSEND_CALLBACK callback)
{
    OnLocalSdpReadyToSend = callback;
}

void SimplePeerConnection::RegisterOnIceCandidateReadyToSend(
    ICECANDIDATEREADYTOSEND_CALLBACK callback)
{
    OnIceCandidateReady = callback;
}

bool SimplePeerConnection::SetRemoteDescription(const char* type, const char* sdp) const
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

bool SimplePeerConnection::AddIceCandidate(const char* candidate, const int sdp_mlineindex, const char* sdp_mid) const
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

bool SimplePeerConnection::SetAudioControl(bool is_mute, bool is_record)
{
    is_mute_audio_ = is_mute;
    is_record_audio_ = is_record;
    return SetAudioControl();
}

bool SimplePeerConnection::SetAudioControl()
{
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
}

void SimplePeerConnection::OnAddStream(
    rtc::scoped_refptr<webrtc::MediaStreamInterface> stream)
{
    RTC_LOG(INFO) << __FUNCTION__ << " " << stream->id();
    remote_stream_ = stream;
    if (remote_video_observer_ && !remote_stream_->GetVideoTracks().empty())
    {
        remote_stream_->GetVideoTracks()[0]->AddOrUpdateSink(
            remote_video_observer_.get(), rtc::VideoSinkWants());
    }

    SetAudioControl();
}

bool SimplePeerConnection::AddStreams(bool audio_only)
{
    if (active_streams_.find(kStreamId) != active_streams_.end())
        return false; // Already added.

    rtc::scoped_refptr<webrtc::MediaStreamInterface> stream =
        g_peer_connection_factory->CreateLocalMediaStream(kStreamId);

    const rtc::scoped_refptr<webrtc::AudioTrackInterface> audio_track(
        g_peer_connection_factory->CreateAudioTrack(
            kAudioLabel, g_peer_connection_factory->CreateAudioSource(
                cricket::AudioOptions())));
    std::string id = audio_track->id();
    stream->AddTrack(audio_track);

    if (!audio_only)
    {
        std::unique_ptr<webrtc::FrameGeneratorCapturer> video_generator;
        video_generator.reset(
            webrtc::FrameGeneratorCapturer::Create(640, 480,
                webrtc::FrameGenerator::OutputType::I420,
                4,
                30,
                webrtc::Clock::GetRealTimeClock()));

        const rtc::scoped_refptr<CapturerTrackSource<webrtc::FrameGeneratorCapturer>> video_device =
            new rtc::RefCountedObject<CapturerTrackSource<webrtc::FrameGeneratorCapturer>>(move(video_generator));

        if (video_device)
        {
            const rtc::scoped_refptr<webrtc::VideoTrackInterface> video_track(
                g_peer_connection_factory->CreateVideoTrack(kVideoLabel,
                    video_device));

            stream->AddTrack(video_track);
        }

        if (local_video_observer_ && !stream->GetVideoTracks().empty())
        {
            stream->GetVideoTracks()[0]->AddOrUpdateSink(local_video_observer_.get(),
                rtc::VideoSinkWants());
        }
    }

    if (!peer_connection_->AddStream(stream))
    {
        RTC_LOG(LS_ERROR) << "Adding stream to PeerConnection failed";
    }

    typedef std::pair<std::string,
        rtc::scoped_refptr<webrtc::MediaStreamInterface>>
        MediaStreamPair;

    active_streams_.insert(MediaStreamPair(stream->id(), stream));

    return true;
}

bool SimplePeerConnection::CreateDataChannel(const char* label, bool is_ordered, bool is_reliable)
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

void SimplePeerConnection::CloseDataChannel(const char* name)
{
    const auto it = data_channels_.find(name);
    if (it == data_channels_.end())
    {
        RTC_LOG(LS_ERROR) << "Data channel '" << name << "' not found";
        return;
    }

    data_channels_.erase(it);
}

bool SimplePeerConnection::SendData(const char* label, const std::string& data)
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

void SimplePeerConnection::OnDataChannel(rtc::scoped_refptr<webrtc::DataChannelInterface> channel)
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
void SimplePeerConnection::OnData(const void* audio_data,
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

std::vector<uint32_t> SimplePeerConnection::GetRemoteAudioTrackSynchronizationSources() const
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

SimplePeerConnection::DataChannelEntry::DataChannelEntry(
    SimplePeerConnection* connection,
    rtc::scoped_refptr<webrtc::DataChannelInterface> data_channel)
    : channel(std::move(std::move(data_channel)))
    , connection(connection)
{
    channel->RegisterObserver(this);
}

SimplePeerConnection::DataChannelEntry::~DataChannelEntry()
{
    if (channel)
    {
        channel->UnregisterObserver();
        channel->Close();
        channel = nullptr;
    }
}

void SimplePeerConnection::DataChannelEntry::OnStateChange()
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
void SimplePeerConnection::DataChannelEntry::OnMessage(const webrtc::DataBuffer& buffer)
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

void SimplePeerConnection::AddRef() const
{
    // Managed by the .NET code, so no-op
}

rtc::RefCountReleaseStatus SimplePeerConnection::Release() const
{
    // Managed by the .NET code, so other refs always remain until disposed or garbage-collected.
    return rtc::RefCountReleaseStatus::kOtherRefsRemained;
}

bool SimplePeerConnection::InitializeThreading(bool use_signaling_thread, bool use_worker_thread)
{
    if (g_worker_thread || g_signaling_thread)
    {
        RTC_LOG(LS_ERROR) << "InitializeThreading must be called before creating the first peer connection";
        return false;
    }

    g_use_signaling_thread = use_signaling_thread;
    g_use_worker_thread = use_worker_thread;
    return true;
}

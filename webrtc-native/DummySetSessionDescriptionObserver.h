#pragma once

class DummySetSessionDescriptionObserver abstract
    : public webrtc::SetSessionDescriptionObserver {
public:
    static DummySetSessionDescriptionObserver* Create();

    void OnSuccess() override;
    void OnFailure(webrtc::RTCError error) override;

protected:
    DummySetSessionDescriptionObserver();
    ~DummySetSessionDescriptionObserver();
};

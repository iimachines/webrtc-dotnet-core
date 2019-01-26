#include "pch.h"
#include "DummySetSessionDescriptionObserver.h"

DummySetSessionDescriptionObserver* DummySetSessionDescriptionObserver::Create()
{
    return new rtc::RefCountedObject<DummySetSessionDescriptionObserver>();
}

DummySetSessionDescriptionObserver::DummySetSessionDescriptionObserver() = default;

DummySetSessionDescriptionObserver::~DummySetSessionDescriptionObserver() = default;

void DummySetSessionDescriptionObserver::OnSuccess()
{
    RTC_LOG(INFO) << __FUNCTION__;
}

void DummySetSessionDescriptionObserver::OnFailure(webrtc::RTCError error)
{
    RTC_LOG(INFO) << __FUNCTION__ << " " << ToString(error.type()) << ": " << error.message();
}


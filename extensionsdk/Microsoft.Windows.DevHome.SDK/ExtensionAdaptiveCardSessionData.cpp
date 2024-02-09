#include "pch.h"
#include "ExtensionAdaptiveCardSessionData.h"
#include "ExtensionAdaptiveCardSessionData.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ExtensionAdaptiveCardSessionData::ExtensionAdaptiveCardSessionData(ExtensionAdaptiveCardSessionEventKind const& eventKind, ProviderOperationResult const& result)
        : m_eventKind(eventKind), m_sessionData(hstring{}), m_timeStamp(winrt::clock::now()), m_result(result)
    {
    }

    ExtensionAdaptiveCardSessionData::ExtensionAdaptiveCardSessionData(ExtensionAdaptiveCardSessionEventKind const& eventKind, hstring const& sessionData, ProviderOperationResult const& result) 
        : m_eventKind(eventKind), m_sessionData(sessionData), m_timeStamp(winrt::clock::now()), m_result(result)
    {
    }

    ExtensionAdaptiveCardSessionEventKind ExtensionAdaptiveCardSessionData::EventKind()
    {
        return m_eventKind;
    }

    hstring ExtensionAdaptiveCardSessionData::SessionData()
    {
        return m_sessionData;
    }

    DateTime ExtensionAdaptiveCardSessionData::TimeStamp()
    {
        return m_timeStamp;
    }

    ProviderOperationResult ExtensionAdaptiveCardSessionData::Result()
    {
        return m_result;
    }
}

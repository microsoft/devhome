#pragma once
#include "ExtensionAdaptiveCardSessionData.g.h"

using namespace winrt::Windows::Foundation;

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ExtensionAdaptiveCardSessionData : ExtensionAdaptiveCardSessionDataT<ExtensionAdaptiveCardSessionData>
    {
        ExtensionAdaptiveCardSessionData(ExtensionAdaptiveCardSessionEventKind const& eventKind, ProviderOperationResult const& result);
        ExtensionAdaptiveCardSessionData(ExtensionAdaptiveCardSessionEventKind const& eventKind, hstring const& sessionData, ProviderOperationResult const& result);
        ExtensionAdaptiveCardSessionEventKind EventKind();
        hstring SessionData();
        DateTime TimeStamp();
        ProviderOperationResult Result();

        private:
            ExtensionAdaptiveCardSessionEventKind m_eventKind;
            hstring m_sessionData;
            DateTime m_timeStamp;
            ProviderOperationResult m_result;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ExtensionAdaptiveCardSessionData : ExtensionAdaptiveCardSessionDataT<ExtensionAdaptiveCardSessionData, implementation::ExtensionAdaptiveCardSessionData>
    {
    };
}

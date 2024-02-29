#include "pch.h"
#include "ExtensionAdaptiveCardSessionStoppedEventArgs.h"
#include "ExtensionAdaptiveCardSessionStoppedEventArgs.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ExtensionAdaptiveCardSessionStoppedEventArgs::ExtensionAdaptiveCardSessionStoppedEventArgs(ProviderOperationResult const& result, hstring const& resultJson) :
        m_result(result), m_resultJson(resultJson)
    {
    }

    hstring ExtensionAdaptiveCardSessionStoppedEventArgs::ResultJson()
    {
        return m_resultJson;
    }

    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult ExtensionAdaptiveCardSessionStoppedEventArgs::Result()
    {
        return m_result;
    }
}

#pragma once
#include "ExtensionAdaptiveCardSessionStoppedEventArgs.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ExtensionAdaptiveCardSessionStoppedEventArgs : ExtensionAdaptiveCardSessionStoppedEventArgsT<ExtensionAdaptiveCardSessionStoppedEventArgs>
    {
        ExtensionAdaptiveCardSessionStoppedEventArgs(ProviderOperationResult const& result, hstring const& resultJson);
        
        hstring ResultJson();
        ProviderOperationResult Result();

        
    private:
        hstring m_resultJson;
        ProviderOperationResult m_result;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ExtensionAdaptiveCardSessionStoppedEventArgs : ExtensionAdaptiveCardSessionStoppedEventArgsT<ExtensionAdaptiveCardSessionStoppedEventArgs, implementation::ExtensionAdaptiveCardSessionStoppedEventArgs>
    {
    };
}

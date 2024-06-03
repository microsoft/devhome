#pragma once
#include "ConfigurationSetStateChangedEventArgs.g.h"

namespace Projection =  winrt::Microsoft::Windows::DevHome::SDK;

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ConfigurationSetStateChangedEventArgs : ConfigurationSetStateChangedEventArgsT<ConfigurationSetStateChangedEventArgs>
    {
        ConfigurationSetStateChangedEventArgs(ConfigurationSetChangeData const& configurationSetChangeData);
        ConfigurationSetChangeData ConfigurationSetChangeData();

    private:
        Projection::ConfigurationSetChangeData m_configurationSetChangeData;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ConfigurationSetStateChangedEventArgs : ConfigurationSetStateChangedEventArgsT<ConfigurationSetStateChangedEventArgs, implementation::ConfigurationSetStateChangedEventArgs>
    {
    };
}

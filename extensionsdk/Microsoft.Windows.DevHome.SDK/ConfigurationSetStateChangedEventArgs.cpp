#include "pch.h"
#include "ConfigurationSetStateChangedEventArgs.h"
#include "ConfigurationSetStateChangedEventArgs.g.cpp"


namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ConfigurationSetStateChangedEventArgs::ConfigurationSetStateChangedEventArgs(Projection::ConfigurationSetChangeData const& configurationSetChangeData) :
        m_configurationSetChangeData(configurationSetChangeData)
    {
    }

    Projection::ConfigurationSetChangeData ConfigurationSetStateChangedEventArgs::ConfigurationSetChangeData()
    {
        return m_configurationSetChangeData;
    }
}

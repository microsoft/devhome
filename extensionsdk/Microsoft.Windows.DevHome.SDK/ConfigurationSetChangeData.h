#pragma once
#include "ConfigurationSetChangeData.g.h"


namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ConfigurationSetChangeData : ConfigurationSetChangeDataT<ConfigurationSetChangeData>
    {
        ConfigurationSetChangeData(
            ConfigurationSetChangeEventType const& change,
            ConfigurationSetState const& setState,
            ConfigurationUnitState const& unitState,
            ConfigurationUnitResultInformation const& resultInformation,
            ConfigurationUnit const& unit);

        ConfigurationSetChangeEventType Change();
        ConfigurationSetState SetState();
        ConfigurationUnitState UnitState();
        ConfigurationUnitResultInformation ResultInformation();
        ConfigurationUnit Unit();

    private:
        ConfigurationSetChangeEventType m_change;
        ConfigurationSetState m_setState;
        ConfigurationUnitState m_unitState;
        ConfigurationUnitResultInformation m_resultInformation;
        ConfigurationUnit m_unit;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ConfigurationSetChangeData : ConfigurationSetChangeDataT<ConfigurationSetChangeData, implementation::ConfigurationSetChangeData>
    {
    };
}

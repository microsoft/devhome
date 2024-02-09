#pragma once
#include "ConfigurationUnit.g.h"

using namespace winrt::Windows::Foundation::Collections;

namespace DevHomeSDKProjection = winrt::Microsoft::Windows::DevHome::SDK;

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ConfigurationUnit : ConfigurationUnitT<ConfigurationUnit>
    {
        ConfigurationUnit(hstring const& type, hstring const& identifier, ConfigurationUnitState const& state, bool isGroup, IVector<DevHomeSDKProjection::ConfigurationUnit> const& units);
        hstring Type();
        hstring Identifier();
        ConfigurationUnitState State();
        bool IsGroup();
        IVector<DevHomeSDKProjection::ConfigurationUnit> Units();

    private:
        hstring m_type;
        hstring m_identifier;
        ConfigurationUnitState m_state;
        bool m_isGroup;
        IVector<DevHomeSDKProjection::ConfigurationUnit> m_units;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ConfigurationUnit : ConfigurationUnitT<ConfigurationUnit, implementation::ConfigurationUnit>
    {
    };
}

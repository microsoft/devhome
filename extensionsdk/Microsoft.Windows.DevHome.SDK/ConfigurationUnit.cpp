#include "pch.h"
#include "ConfigurationUnit.h"
#include "ConfigurationUnit.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ConfigurationUnit::ConfigurationUnit(
        hstring const& type,
        hstring const& identifier,
        ConfigurationUnitState const& state,
        bool isGroup,
        IVector<DevHomeSDKProjection::ConfigurationUnit> const& units,
        ValueSet const& settings,
        DevHomeSDKProjection::ConfigurationUnitIntent const& intent) :
        m_type(type), m_identifier(identifier), m_state(state), m_isGroup(isGroup), m_units(units), m_settings(settings), m_intent(intent)
    {
    }

    hstring ConfigurationUnit::Type()
    {
        return m_type;
    }

    hstring ConfigurationUnit::Identifier()
    {
        return m_identifier;
    }

    ConfigurationUnitState ConfigurationUnit::State()
    {
        return m_state;
    }

    bool ConfigurationUnit::IsGroup()
    {
        return m_isGroup;
    }

    IVector<DevHomeSDKProjection::ConfigurationUnit> ConfigurationUnit::Units()
    {
        return m_units;
    }

    ValueSet ConfigurationUnit::Settings()
    {
        return m_settings;
    }

    DevHomeSDKProjection::ConfigurationUnitIntent ConfigurationUnit::Intent()
    {
        return m_intent;
    }
}

#include "pch.h"
#include "ConfigurationSetChangeData.h"
#include "ConfigurationSetChangeData.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ConfigurationSetChangeData::ConfigurationSetChangeData(
        ConfigurationSetChangeEventType const& change,
        ConfigurationSetState const& setState,
        ConfigurationUnitState const& unitState,
        ConfigurationUnitResultInformation const& resultInformation,
        ConfigurationUnit const& unit,
        IExtensionAdaptiveCardSession2 const& correctiveActionCardSession)
        : m_change(change), m_setState(setState), m_unitState(unitState), m_resultInformation(resultInformation), m_unit(unit), m_correctiveActionCardSession(correctiveActionCardSession)
    {
    }

    ConfigurationSetChangeEventType ConfigurationSetChangeData::Change()
    {
        return m_change;
    }

    ConfigurationSetState ConfigurationSetChangeData::SetState()
    {
        return m_setState;
    }

    ConfigurationUnitState ConfigurationSetChangeData::UnitState()
    {
        return m_unitState;
    }

    ConfigurationUnitResultInformation ConfigurationSetChangeData::ResultInformation()
    {
        return m_resultInformation;
    }

    ConfigurationUnit ConfigurationSetChangeData::Unit()
    {
        return m_unit;
    }

    IExtensionAdaptiveCardSession2 ConfigurationSetChangeData::CorrectiveActionCardSession()
    {
        return m_correctiveActionCardSession;
    }
}

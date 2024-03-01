#include "pch.h"
#include "ApplyConfigurationUnitResult.h"
#include "ApplyConfigurationUnitResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ApplyConfigurationUnitResult::ApplyConfigurationUnitResult(ConfigurationUnit const& unit, ConfigurationUnitState const& state, bool previouslyInDesiredState, bool rebootRequired, ConfigurationUnitResultInformation const& resultInformation) :
        m_unit(unit), m_previouslyInDesiredState(previouslyInDesiredState), m_rebootRequired(rebootRequired), m_resultInformation(resultInformation), m_state(state)
    {
    }

    ConfigurationUnit ApplyConfigurationUnitResult::Unit()
    {
        return m_unit;
    }
    bool ApplyConfigurationUnitResult::PreviouslyInDesiredState()
    {
        return m_previouslyInDesiredState;
    }
    bool ApplyConfigurationUnitResult::RebootRequired()
    {
        return m_rebootRequired;
    }
    ConfigurationUnitResultInformation ApplyConfigurationUnitResult::ResultInformation()
    {
        return m_resultInformation;
    }

    ConfigurationUnitState ApplyConfigurationUnitResult::State()
    {
        return m_state;
    }
}
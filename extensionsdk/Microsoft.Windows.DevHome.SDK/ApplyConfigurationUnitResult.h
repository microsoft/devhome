#pragma once
#include "ApplyConfigurationUnitResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ApplyConfigurationUnitResult : ApplyConfigurationUnitResultT<ApplyConfigurationUnitResult>
    {
        ApplyConfigurationUnitResult(ConfigurationUnit const& unit, bool previouslyInDesiredState, bool rebootRequired, ConfigurationUnitResultInformation const& resultInformation);
        ConfigurationUnit Unit();
        bool PreviouslyInDesiredState();
        bool RebootRequired();
        ConfigurationUnitResultInformation ResultInformation();

    private:
        ConfigurationUnit m_unit{ nullptr };
        ConfigurationUnitState m_state{ ConfigurationUnitState::Unknown };
        bool m_previouslyInDesiredState{ false };
        bool m_rebootRequired{ false };
        ConfigurationUnitResultInformation m_resultInformation{ nullptr };
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ApplyConfigurationUnitResult : ApplyConfigurationUnitResultT<ApplyConfigurationUnitResult, implementation::ApplyConfigurationUnitResult>
    {
    };
}

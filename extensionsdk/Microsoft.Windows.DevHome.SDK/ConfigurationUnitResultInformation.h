#pragma once
#include "ConfigurationUnitResultInformation.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ConfigurationUnitResultInformation : ConfigurationUnitResultInformationT<ConfigurationUnitResultInformation>
    {
        ConfigurationUnitResultInformation(winrt::hresult const& result, hstring const& description, hstring const& details, ConfigurationUnitResultSource const& resultSource);
        winrt::hresult ResultCode();
        hstring Description();
        hstring Details();
        ConfigurationUnitResultSource ResultSource();

    private:
        winrt::hresult m_resultCode;
        hstring m_description;
        hstring m_details;
        ConfigurationUnitResultSource m_resultSource;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ConfigurationUnitResultInformation : ConfigurationUnitResultInformationT<ConfigurationUnitResultInformation, implementation::ConfigurationUnitResultInformation>
    {
    };
}

#include "pch.h"
#include "ConfigurationUnitResultInformation.h"
#include "ConfigurationUnitResultInformation.g.cpp"


namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ConfigurationUnitResultInformation::ConfigurationUnitResultInformation(
        winrt::hresult const& result,
        hstring const& description,
        hstring const& details,
        ConfigurationUnitResultSource const& resultSource) :
        m_resultCode(result), m_description(description), m_details(details), m_resultSource(resultSource)
    {
    }

    winrt::hresult ConfigurationUnitResultInformation::ResultCode()
    {
        return m_resultCode;
    }

    hstring ConfigurationUnitResultInformation::Description()
    {
        return m_description;
    }

    hstring ConfigurationUnitResultInformation::Details()
    {
        return m_details;
    }

    ConfigurationUnitResultSource ConfigurationUnitResultInformation::ResultSource()
    {
        return m_resultSource;
    }
}

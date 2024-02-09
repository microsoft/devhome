#include "pch.h"
#include "ApplyConfigurationResult.h"
#include "ApplyConfigurationResult.g.cpp"

namespace DevHomeSDKProjection = winrt::Microsoft::Windows::DevHome::SDK;

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ApplyConfigurationResult::ApplyConfigurationResult(
        winrt::hresult const& result,
        winrt::hstring const& resultDescription,
        DevHomeSDKProjection::OpenConfigurationSetResult const& openConfigurationSetResult,
        DevHomeSDKProjection::ApplyConfigurationSetResult const& applyConfigurationSetResult)
            : m_resultCode(result), m_resultDescription(resultDescription), m_openConfigurationSetResult(openConfigurationSetResult), m_applyConfigurationSetResult(applyConfigurationSetResult)
    {
    }

    winrt::hresult ApplyConfigurationResult::ResultCode()
    {
        return m_resultCode;
    }

    hstring ApplyConfigurationResult::ResultDescription()
    {
        return m_resultDescription;
    }

    OpenConfigurationSetResult ApplyConfigurationResult::OpenConfigurationSetResult()
    {
        return m_openConfigurationSetResult;
    }
    ApplyConfigurationSetResult ApplyConfigurationResult::ApplyConfigurationSetResult()
    {
        return m_applyConfigurationSetResult;
    }
}

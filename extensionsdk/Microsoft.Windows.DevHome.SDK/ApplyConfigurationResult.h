#pragma once
#include "ApplyConfigurationResult.g.h"

namespace DevHomeSDKProjection = winrt::Microsoft::Windows::DevHome::SDK;

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ApplyConfigurationResult : ApplyConfigurationResultT<ApplyConfigurationResult>
    {
        ApplyConfigurationResult(
            winrt::hresult const& result,
            winrt::hstring const& resultDescription,
            DevHomeSDKProjection::OpenConfigurationSetResult const& openConfigurationSetResult,
            DevHomeSDKProjection::ApplyConfigurationSetResult const& applyConfigurationSetResult);

        winrt::hresult ResultCode();
        hstring ResultDescription();
        OpenConfigurationSetResult OpenConfigurationSetResult();
        ApplyConfigurationSetResult ApplyConfigurationSetResult();

    private:
        winrt::hresult m_resultCode{ S_OK };
        winrt::hstring m_resultDescription;
        DevHomeSDKProjection::OpenConfigurationSetResult m_openConfigurationSetResult{ nullptr };
        DevHomeSDKProjection::ApplyConfigurationSetResult m_applyConfigurationSetResult{ nullptr };
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ApplyConfigurationResult : ApplyConfigurationResultT<ApplyConfigurationResult, implementation::ApplyConfigurationResult>
    {
    };
}

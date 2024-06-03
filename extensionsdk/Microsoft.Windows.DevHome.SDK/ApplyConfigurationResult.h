#pragma once
#include "ApplyConfigurationResult.g.h"

namespace Projection = winrt::Microsoft::Windows::DevHome::SDK;

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ApplyConfigurationResult : ApplyConfigurationResultT<ApplyConfigurationResult>
    {
        ApplyConfigurationResult(Projection::OpenConfigurationSetResult const& openConfigurationSetResult, Projection::ApplyConfigurationSetResult const& applyConfigurationSetResult);
        ApplyConfigurationResult(winrt::hresult const& e, hstring const& displayMessage, hstring const& diagnosticText);

        ProviderOperationResult Result();
        Projection::OpenConfigurationSetResult OpenConfigurationSetResult();
        Projection::ApplyConfigurationSetResult ApplyConfigurationSetResult();

    private:
        ProviderOperationResult m_result;
        Projection::OpenConfigurationSetResult m_openConfigurationSetResult{ nullptr };
        Projection::ApplyConfigurationSetResult m_applyConfigurationSetResult{ nullptr };
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ApplyConfigurationResult : ApplyConfigurationResultT<ApplyConfigurationResult, implementation::ApplyConfigurationResult>
    {
    };
}

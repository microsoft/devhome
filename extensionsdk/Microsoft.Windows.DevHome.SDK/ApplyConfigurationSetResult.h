#pragma once
#include "ApplyConfigurationSetResult.g.h"

namespace DevHomeSDKProjection = winrt::Microsoft::Windows::DevHome::SDK;

using namespace winrt::Windows::Foundation::Collections;

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ApplyConfigurationSetResult : ApplyConfigurationSetResultT<ApplyConfigurationSetResult>
    {
        ApplyConfigurationSetResult(winrt::hresult const& resultCode, IVectorView<DevHomeSDKProjection::ApplyConfigurationUnitResult> const& unitResults);
        IVectorView<DevHomeSDKProjection::ApplyConfigurationUnitResult> UnitResults();
        winrt::hresult ResultCode();

    private:
        winrt::hresult m_resultCode{ S_OK };
        IVectorView<DevHomeSDKProjection::ApplyConfigurationUnitResult> m_unitResults{ nullptr };
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ApplyConfigurationSetResult : ApplyConfigurationSetResultT<ApplyConfigurationSetResult, implementation::ApplyConfigurationSetResult>
    {
    };
}

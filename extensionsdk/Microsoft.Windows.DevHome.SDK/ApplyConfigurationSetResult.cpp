#include "pch.h"
#include "ApplyConfigurationSetResult.h"
#include "ApplyConfigurationSetResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ApplyConfigurationSetResult::ApplyConfigurationSetResult(winrt::hresult const& resultCode, IVectorView<DevHomeSDKProjection::ApplyConfigurationUnitResult> const& unitResults) :
        m_resultCode(resultCode), m_unitResults(unitResults)
    {
    }

    IVectorView<DevHomeSDKProjection::ApplyConfigurationUnitResult> ApplyConfigurationSetResult::UnitResults()
    {
        return m_unitResults;
    }

    winrt::hresult ApplyConfigurationSetResult::ResultCode()
    {
        return m_resultCode;
    }
}

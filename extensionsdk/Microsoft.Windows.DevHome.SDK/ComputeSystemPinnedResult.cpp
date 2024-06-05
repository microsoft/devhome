#include "pch.h"
#include "ComputeSystemPinnedResult.h"
#include "ComputeSystemPinnedResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ComputeSystemPinnedResult::ComputeSystemPinnedResult(bool isPinned) :
        m_isPinned(isPinned), m_result(ProviderOperationStatus::Success, S_OK, hstring(), hstring())
    {
    }

    ComputeSystemPinnedResult::ComputeSystemPinnedResult(winrt::hresult const& e, hstring const& displayMessage, hstring const& diagnosticText) :
        m_isPinned(false), m_result(ProviderOperationStatus::Failure, e, displayMessage, diagnosticText)
    {
    }

    bool ComputeSystemPinnedResult::IsPinned()
    {
        return m_isPinned;
    }

    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult ComputeSystemPinnedResult::Result()
    {
        return m_result;
    }
}

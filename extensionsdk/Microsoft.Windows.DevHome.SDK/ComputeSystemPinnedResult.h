#pragma once
#include "ComputeSystemPinnedResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ComputeSystemPinnedResult : ComputeSystemPinnedResultT<ComputeSystemPinnedResult>
    {
        ComputeSystemPinnedResult() = default;

        ComputeSystemPinnedResult(bool isPinned);
        ComputeSystemPinnedResult(winrt::hresult const& e, hstring const& displayMessage, hstring const& diagnosticText);
        bool IsPinned();
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult Result();

    private:
        bool m_isPinned;
        ProviderOperationResult m_result;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ComputeSystemPinnedResult : ComputeSystemPinnedResultT<ComputeSystemPinnedResult, implementation::ComputeSystemPinnedResult>
    {
    };
}

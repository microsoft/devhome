#pragma once
#include "ComputeSystemAdaptiveCardResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ComputeSystemAdaptiveCardResult : ComputeSystemAdaptiveCardResultT<ComputeSystemAdaptiveCardResult>
    {
        ComputeSystemAdaptiveCardResult(IExtensionAdaptiveCardSession2 const& cardSession);
        ComputeSystemAdaptiveCardResult(winrt::hresult const& e, hstring const& displayMessage, hstring const& diagnosticText);
        IExtensionAdaptiveCardSession2 ComputeSystemCardSession();
        ProviderOperationResult Result();

    private:
        IExtensionAdaptiveCardSession2 m_computeSystemCardSession;
        ProviderOperationResult m_result;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ComputeSystemAdaptiveCardResult : ComputeSystemAdaptiveCardResultT<ComputeSystemAdaptiveCardResult, implementation::ComputeSystemAdaptiveCardResult>
    {
    };
}

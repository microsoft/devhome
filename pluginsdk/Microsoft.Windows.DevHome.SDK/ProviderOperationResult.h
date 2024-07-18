#pragma once
#include "ProviderOperationResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ProviderOperationResult : ProviderOperationResultT<ProviderOperationResult>
    {
        ProviderOperationResult() = default;

        ProviderOperationResult(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus const& status, winrt::hresult const& error, hstring const& displayMessage, hstring const& diagnosticText);
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus Status();
        winrt::hresult ExtendedError();
        hstring DisplayMessage();
        hstring DiagnosticText();
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ProviderOperationResult : ProviderOperationResultT<ProviderOperationResult, implementation::ProviderOperationResult>
    {
    };
}

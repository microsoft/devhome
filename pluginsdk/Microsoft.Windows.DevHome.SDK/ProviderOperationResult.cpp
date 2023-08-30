#include "pch.h"
#include "ProviderOperationResult.h"
#include "ProviderOperationResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ProviderOperationResult::ProviderOperationResult(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus const& status, winrt::hresult const& error, hstring const& displayMessage, hstring const& diagnosticText)
    {
        throw hresult_not_implemented();
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus ProviderOperationResult::Status()
    {
        throw hresult_not_implemented();
    }
    winrt::hresult ProviderOperationResult::ExtendedError()
    {
        throw hresult_not_implemented();
    }
    hstring ProviderOperationResult::DisplayMessage()
    {
        throw hresult_not_implemented();
    }
    hstring ProviderOperationResult::DiagnosticText()
    {
        throw hresult_not_implemented();
    }
}

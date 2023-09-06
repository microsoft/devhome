#include "pch.h"
#include "ProviderOperationResult.h"
#include "ProviderOperationResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ProviderOperationResult::ProviderOperationResult(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus const& status, winrt::hresult const& error, hstring const& displayMessage, hstring const& diagnosticText) :
        _Status(status), _ExtendedError(error), _DisplayMessage(displayMessage), _DiagnosticText(diagnosticText)
    {
    }

    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus ProviderOperationResult::Status()
    {
        return _Status;
    }

    winrt::hresult ProviderOperationResult::ExtendedError()
    {
        return _ExtendedError;
    }

    hstring ProviderOperationResult::DisplayMessage()
    {
        return _DisplayMessage;
    }

    hstring ProviderOperationResult::DiagnosticText()
    {
        return _DiagnosticText;
    }
}

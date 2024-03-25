#include "pch.h"
#include "RepositoryUriSupportResult.h"
#include "RepositoryUriSupportResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    RepositoryUriSupportResult::RepositoryUriSupportResult(bool isSupported)
    {
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Success, winrt::hresult(S_OK), winrt::to_hstring(""), winrt::to_hstring(""));
        _IsSupported = isSupported;
    }

    RepositoryUriSupportResult::RepositoryUriSupportResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Failure, winrt::hresult(e), diagnosticText, diagnosticText);
        _IsSupported = false;
    }

    bool RepositoryUriSupportResult::IsSupported()
    {
        return _IsSupported;
    }

    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult RepositoryUriSupportResult::Result()
    {
        return *_Result.get();
    }
}

#include "pch.h"
#include "RepositoriesResult.h"
#include "RepositoriesResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    RepositoriesResult::RepositoriesResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> const& repositories)
    {
        _Repositories = std::make_shared<winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository>>(repositories);
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Success, winrt::hresult(S_OK), winrt::to_hstring(""), winrt::to_hstring(""));
    }

    RepositoriesResult::RepositoriesResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Failure, winrt::hresult(e), winrt::to_hstring("Something went wrong"), diagnosticText);
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Failure, winrt::hresult(e), diagnosticText, diagnosticText);
    }

    winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> RepositoriesResult::Repositories()
    {
        return *_Repositories.get();
    }

    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult RepositoriesResult::Result()
    {
        return *_Result.get();
    }
}

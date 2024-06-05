#include "pch.h"
#include "RepositoryResult.h"
#include "RepositoryResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    RepositoryResult::RepositoryResult(winrt::Microsoft::Windows::DevHome::SDK::IRepository const& repository)
    {
        _Repository = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::IRepository>(repository);
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Success, winrt::hresult(S_OK), winrt::to_hstring(""), winrt::to_hstring(""));
    }
    RepositoryResult::RepositoryResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        _Repository = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::IRepository>();
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Failure, winrt::hresult(e), diagnosticText, diagnosticText);
    }
    winrt::Microsoft::Windows::DevHome::SDK::IRepository RepositoryResult::Repository()
    {
        return *_Repository.get();
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult RepositoryResult::Result()
    {
        return *_Result.get();
    }
}

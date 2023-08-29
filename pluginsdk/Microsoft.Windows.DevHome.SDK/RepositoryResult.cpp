#include "pch.h"
#include "RepositoryResult.h"
#include "RepositoryResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    RepositoryResult::RepositoryResult(winrt::Microsoft::Windows::DevHome::SDK::IRepository const& repository)
    {
        throw hresult_not_implemented();
    }
    RepositoryResult::RepositoryResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        throw hresult_not_implemented();
    }
    winrt::Microsoft::Windows::DevHome::SDK::IRepository RepositoryResult::Repository()
    {
        throw hresult_not_implemented();
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult RepositoryResult::Result()
    {
        throw hresult_not_implemented();
    }
}

#include "pch.h"
#include "RepositoriesResult.h"
#include "RepositoriesResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    RepositoriesResult::RepositoriesResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> const& repositories)
    {
        throw hresult_not_implemented();
    }
    RepositoriesResult::RepositoriesResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        throw hresult_not_implemented();
    }
    winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> RepositoriesResult::Repositories()
    {
        throw hresult_not_implemented();
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult RepositoriesResult::Result()
    {
        throw hresult_not_implemented();
    }
}

#pragma once
#include "RepositoriesResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct RepositoriesResult : RepositoriesResultT<RepositoriesResult>
    {
        RepositoriesResult() = default;

        RepositoriesResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> const& repositories);
        RepositoriesResult(winrt::hresult const& e, hstring const& diagnosticText);
        winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IRepository> Repositories();
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult Result();
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct RepositoriesResult : RepositoriesResultT<RepositoriesResult, implementation::RepositoriesResult>
    {
    };
}

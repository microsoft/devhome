#pragma once
#include "RepositoryResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct RepositoryResult : RepositoryResultT<RepositoryResult>
    {
        RepositoryResult() = default;

        RepositoryResult(winrt::Microsoft::Windows::DevHome::SDK::IRepository const& repository);
        RepositoryResult(winrt::hresult const& e, hstring const& diagnosticText);
        winrt::Microsoft::Windows::DevHome::SDK::IRepository Repository();
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult Result();
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct RepositoryResult : RepositoryResultT<RepositoryResult, implementation::RepositoryResult>
    {
    };
}

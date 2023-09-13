#pragma once
#include "DeveloperIdsResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct DeveloperIdsResult : DeveloperIdsResultT<DeveloperIdsResult>
    {
        DeveloperIdsResult() = default;

        DeveloperIdsResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IDeveloperId> const& developerIds);
        DeveloperIdsResult(winrt::hresult const& e, hstring const& diagnosticText);
        winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IDeveloperId> DeveloperIds();
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult Result();
    
    private:
        winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IDeveloperId> _DeveloperIds;
        std::shared_ptr<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult> _Result;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct DeveloperIdsResult : DeveloperIdsResultT<DeveloperIdsResult, implementation::DeveloperIdsResult>
    {
    };
}

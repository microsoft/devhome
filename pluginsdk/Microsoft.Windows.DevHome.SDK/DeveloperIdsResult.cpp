#include "pch.h"
#include "DeveloperIdsResult.h"
#include "DeveloperIdsResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    DeveloperIdsResult::DeveloperIdsResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IDeveloperId> const& developerIds)
    {
        throw hresult_not_implemented();
    }
    DeveloperIdsResult::DeveloperIdsResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        throw hresult_not_implemented();
    }
    winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IDeveloperId> DeveloperIdsResult::DeveloperIds()
    {
        throw hresult_not_implemented();
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult DeveloperIdsResult::Result()
    {
        throw hresult_not_implemented();
    }
}

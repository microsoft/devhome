#include "pch.h"
#include "DeveloperIdsResult.h"
#include "DeveloperIdsResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    DeveloperIdsResult::DeveloperIdsResult(winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IDeveloperId> const& developerIds) :
        _DeveloperIds(developerIds)
    {
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Success, winrt::hresult(S_OK), winrt::to_hstring("Operation successful"), winrt::to_hstring("Operation Successful"));
    }

    DeveloperIdsResult::DeveloperIdsResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Failure, winrt::hresult(e), winrt::to_hstring("Could not get developer ids."), diagnosticText);
        _DeveloperIds = winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IDeveloperId>{ nullptr };
    }

    winrt::Windows::Foundation::Collections::IIterable<winrt::Microsoft::Windows::DevHome::SDK::IDeveloperId> DeveloperIdsResult::DeveloperIds()
    {
        return _DeveloperIds;
    }

    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult DeveloperIdsResult::Result()
    {
        return *_Result.get();
    }
}

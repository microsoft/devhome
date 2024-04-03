#include "pch.h"
#include "GetLocalRepositoryResult.h"
#include "GetLocalRepositoryResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    GetLocalRepositoryResult::GetLocalRepositoryResult(winrt::Microsoft::Windows::DevHome::SDK::ILocalRepository const& repository) :
        _repository(repository), _result(ProviderOperationStatus::Success, S_OK, hstring(), hstring())
    {
    }

    GetLocalRepositoryResult::GetLocalRepositoryResult(winrt::hresult const& e, hstring const& displayMessage, hstring const& diagnosticText) :
        _result(ProviderOperationStatus::Failure, e, displayMessage, diagnosticText)
    {
    }
    winrt::Microsoft::Windows::DevHome::SDK::ILocalRepository GetLocalRepositoryResult::Repository()
    {
        return _repository;
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult GetLocalRepositoryResult::Result()
    {
        return _result;
    }
}
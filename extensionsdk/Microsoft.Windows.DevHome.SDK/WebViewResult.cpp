#include "pch.h"
#include "WebViewResult.h"
#include "WebViewResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    WebViewResult::WebViewResult(hstring const& url) :
        _Url(url)
    {
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Success, winrt::hresult(S_OK), winrt::to_hstring(""), winrt::to_hstring(""));
    }
    WebViewResult::WebViewResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        _Result = std::make_shared<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult>(winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationStatus::Failure, winrt::hresult(e), winrt::to_hstring("Something went wrong"), diagnosticText);
    }
    hstring WebViewResult::Url()
    {
        return _Url;
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult WebViewResult::Result()
    {
        return *_Result.get();
    }
}
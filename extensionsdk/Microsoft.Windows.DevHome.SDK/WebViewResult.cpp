#include "pch.h"
#include "WebViewResult.h"
#include "WebViewResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    WebViewResult::WebViewResult(winrt::Windows::Foundation::Uri const& uri) :
        _Uri(uri)
    {
        _Result = std::make_shared<ProviderOperationResult>(ProviderOperationStatus::Success, winrt::hresult(S_OK), winrt::to_hstring(""), winrt::to_hstring(""));
    }

    WebViewResult::WebViewResult(winrt::hresult const& e, winrt::hstring const& diagnosticText) :
        _Uri(winrt::Windows::Foundation::Uri(L"about:blank"))
    {
        _Result = std::make_shared<ProviderOperationResult>(ProviderOperationStatus::Failure, winrt::hresult(e), winrt::to_hstring("Something went wrong"), diagnosticText);
    }

    Uri WebViewResult::Uri()
    {
        return _Uri;
    }

    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult WebViewResult::Result()
    {
        return *_Result.get();
    }
}

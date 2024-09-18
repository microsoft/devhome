#pragma once
#include "WebViewResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct WebViewResult : WebViewResultT<WebViewResult>
    {
        WebViewResult() = default;

        WebViewResult(hstring const& url);
        WebViewResult(winrt::hresult const& e, hstring const& diagnosticText);
        hstring Url();
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult Result();

    private:
        std::shared_ptr<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult> _Result;
        const hstring _Url;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct WebViewResult : WebViewResultT<WebViewResult, implementation::WebViewResult>
    {
    };
}
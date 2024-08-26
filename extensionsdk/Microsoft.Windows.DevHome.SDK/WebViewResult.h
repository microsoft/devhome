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
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct WebViewResult : WebViewResultT<WebViewResult, implementation::WebViewResult>
    {
    };
}

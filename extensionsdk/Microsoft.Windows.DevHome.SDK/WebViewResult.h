#pragma once
#include "WebViewResult.g.h"

using namespace winrt::Windows::Foundation;

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct WebViewResult : WebViewResultT<WebViewResult>
    {
        WebViewResult(Uri const& uri);
        WebViewResult(winrt::hresult const& e, winrt::hstring const& diagnosticText);
        Uri Uri();
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult Result();

    private:
        std::shared_ptr<winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult> _Result;
        winrt::Windows::Foundation::Uri _Uri;
    };
}

namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct WebViewResult : WebViewResultT<WebViewResult, implementation::WebViewResult>
    {
    };
}

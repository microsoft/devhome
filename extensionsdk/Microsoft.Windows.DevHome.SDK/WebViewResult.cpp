#include "pch.h"
#include "WebViewResult.h"
#include "WebViewResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    WebViewResult::WebViewResult(hstring const& url)
    {
        throw hresult_not_implemented();
    }
    WebViewResult::WebViewResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        throw hresult_not_implemented();
    }
    hstring WebViewResult::Url()
    {
        throw hresult_not_implemented();
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult WebViewResult::Result()
    {
        throw hresult_not_implemented();
    }
}

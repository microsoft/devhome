#pragma once
#include "OpenConfigurationSetResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct OpenConfigurationSetResult : OpenConfigurationSetResultT<OpenConfigurationSetResult>
    {
        OpenConfigurationSetResult(winrt::hresult const& result, hstring const& field, hstring const& fieldValue, uint32_t line, uint32_t column);
        
        winrt::hresult ResultCode();
        hstring Field();
        hstring Value();
        uint32_t Line();
        uint32_t Column();

    private:
        winrt::hresult m_resultCode;
        hstring m_field;
        hstring m_fieldValue;
        uint32_t m_line;
        uint32_t m_column;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct OpenConfigurationSetResult : OpenConfigurationSetResultT<OpenConfigurationSetResult, implementation::OpenConfigurationSetResult>
    {
    };
}

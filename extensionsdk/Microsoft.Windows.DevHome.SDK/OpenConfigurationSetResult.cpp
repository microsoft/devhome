#include "pch.h"
#include "OpenConfigurationSetResult.h"
#include "OpenConfigurationSetResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    OpenConfigurationSetResult::OpenConfigurationSetResult(winrt::hresult const& resultCode, hstring const& field, hstring const& fieldValue, uint32_t line, uint32_t column) :
        m_resultCode(resultCode), m_field(field), m_fieldValue(fieldValue), m_line(line), m_column(column)
    {
    }

    winrt::hresult OpenConfigurationSetResult::ResultCode()
    {
        return m_resultCode;
    }

    hstring OpenConfigurationSetResult::Field()
    {
        return m_field;
    }

    hstring OpenConfigurationSetResult::Value()
    {
        return m_fieldValue;
    }

    uint32_t OpenConfigurationSetResult::Line()
    {
        return m_line;
    }

    uint32_t OpenConfigurationSetResult::Column()
    {
        return m_column;
    }
}

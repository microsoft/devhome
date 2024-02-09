#include "pch.h"
#include "ComputeSystemProperty.h"
#include "ComputeSystemProperty.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ComputeSystemProperty::ComputeSystemProperty(Uri const& icon, hstring const& propertyName, IInspectable const& propertyValue, ComputeSystemPropertyKind const& propertyKind)
        : m_icon(icon), m_name(propertyName), m_value(propertyValue), m_propertyKind(propertyKind)
    {
    }

    ComputeSystemProperty::ComputeSystemProperty(hstring const& propertyName, IInspectable const& propertyValue, ComputeSystemPropertyKind const& propertyKind)
        : m_icon(nullptr), m_name(propertyName), m_value(propertyValue), m_propertyKind(propertyKind)
    {
    }

    ComputeSystemProperty::ComputeSystemProperty(IInspectable const& propertyValue, ComputeSystemPropertyKind const& propertyKind) :
        m_icon(nullptr), m_name(hstring{}), m_value(propertyValue), m_propertyKind(propertyKind)
    {
    }

    Uri ComputeSystemProperty::Icon()
    {
        return m_icon;
    }

    hstring ComputeSystemProperty::Name()
    {
        return m_name;
    }

    winrt::Windows::Foundation::IInspectable ComputeSystemProperty::Value()
    {
        return m_value;
    }

    ComputeSystemPropertyKind ComputeSystemProperty::PropertyKind()
    {
        return m_propertyKind;
    }
}
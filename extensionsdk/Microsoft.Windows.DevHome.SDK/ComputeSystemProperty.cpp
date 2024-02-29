#include "pch.h"
#include "ComputeSystemProperty.h"
#include "ComputeSystemProperty.g.cpp"


namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    // Create a compute system property that is predefined by Dev Home based on the ComputeSystemPropertyKind value passed in.
    ComputeSystemProperty::ComputeSystemProperty(Projection::ComputeSystemPropertyKind const& propertyKind, IInspectable const& propertyValue) :
        m_propertyKind(propertyKind), m_value(propertyValue), m_icon(nullptr), m_name(L"")
    {
    }

    // Creates a custom compute system property.
    ComputeSystemProperty::ComputeSystemProperty(IInspectable const& propertyValue, hstring const& propertyName, Uri const& icon) :
        m_value(propertyValue), m_name(propertyName), m_icon(icon), m_propertyKind(Projection::ComputeSystemPropertyKind::Custom)
    {
    }

    Projection::ComputeSystemProperty ComputeSystemProperty::Create(Projection::ComputeSystemPropertyKind const& propertyKind, IInspectable const& propertyValue)
    {
        if (propertyKind == Projection::ComputeSystemPropertyKind::Custom)
        {
            throw hresult_invalid_argument(L"propertyKind parameter should not be 'Custom'. Use CreateCustom method instead.");
        }

        return make<ComputeSystemProperty>(propertyKind, propertyValue);
    }

    Projection::ComputeSystemProperty ComputeSystemProperty::CreateCustom(IInspectable const& propertyValue, hstring const& propertyName, Uri const& icon)
    {
        return make<ComputeSystemProperty>(propertyValue, propertyName, icon);
    }

    Uri ComputeSystemProperty::Icon()
    {
        return m_icon;
    }

    hstring ComputeSystemProperty::Name()
    {
        return m_name;
    }

    IInspectable ComputeSystemProperty::Value()
    {
        return m_value;
    }

    ComputeSystemPropertyKind ComputeSystemProperty::PropertyKind()
    {
        return m_propertyKind;
    }
}

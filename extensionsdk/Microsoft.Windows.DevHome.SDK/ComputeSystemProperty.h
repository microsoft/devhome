#pragma once
#include "ComputeSystemProperty.g.h"

using namespace winrt::Windows::Foundation;

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ComputeSystemProperty : ComputeSystemPropertyT<ComputeSystemProperty>
    {
        ComputeSystemProperty(Uri const& icon, hstring const& propertyName, IInspectable const& propertyValue, ComputeSystemPropertyKind const& propertyKind);
        ComputeSystemProperty(hstring const& propertyName, IInspectable const& propertyValue, ComputeSystemPropertyKind const& propertyKind);
        ComputeSystemProperty(IInspectable const& propertyValue, ComputeSystemPropertyKind const& propertyKind);
        Uri Icon();
        hstring Name();
        IInspectable Value();
        ComputeSystemPropertyKind PropertyKind();

        private:
            Uri m_icon;
            hstring m_name;
            IInspectable m_value;
            ComputeSystemPropertyKind m_propertyKind;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ComputeSystemProperty : ComputeSystemPropertyT<ComputeSystemProperty, implementation::ComputeSystemProperty>
    {
    };
}

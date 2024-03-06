#pragma once
#include "ComputeSystemProperty.g.h"

using namespace winrt::Windows::Foundation;
namespace Projection = winrt::Microsoft::Windows::DevHome::SDK;

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ComputeSystemProperty : ComputeSystemPropertyT<ComputeSystemProperty>
    {
        ComputeSystemProperty(ComputeSystemPropertyKind const& propertyKind, IInspectable const& propertyValue);
        ComputeSystemProperty(IInspectable const& propertyValue, hstring const& propertyName, Uri const& icon);

        static Projection::ComputeSystemProperty Create(Projection::ComputeSystemPropertyKind const& propertyKind, IInspectable const& propertyValue);
        static Projection::ComputeSystemProperty CreateCustom(IInspectable const& propertyValue, hstring const& propertyName, Uri const& icon);

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

#pragma once
#include "CreateComputeSystemActionRequiredEventArgs.g.h"


namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct CreateComputeSystemActionRequiredEventArgs : CreateComputeSystemActionRequiredEventArgsT<CreateComputeSystemActionRequiredEventArgs>
    {
        CreateComputeSystemActionRequiredEventArgs(IExtensionAdaptiveCardSession2 const& correctiveActionCardSession);

        IExtensionAdaptiveCardSession2 CorrectiveActionCardSession();

    private:
        IExtensionAdaptiveCardSession2 m_correctiveActionCardSession;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct CreateComputeSystemActionRequiredEventArgs : CreateComputeSystemActionRequiredEventArgsT<CreateComputeSystemActionRequiredEventArgs, implementation::CreateComputeSystemActionRequiredEventArgs>
    {
    };
}

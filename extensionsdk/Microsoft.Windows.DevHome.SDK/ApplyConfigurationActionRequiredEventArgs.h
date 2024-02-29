#pragma once
#include "ApplyConfigurationActionRequiredEventArgs.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ApplyConfigurationActionRequiredEventArgs : ApplyConfigurationActionRequiredEventArgsT<ApplyConfigurationActionRequiredEventArgs>
    {
        ApplyConfigurationActionRequiredEventArgs(IExtensionAdaptiveCardSession2 const& correctiveActionCardSession);
        IExtensionAdaptiveCardSession2 CorrectiveActionCardSession();

        private:
            IExtensionAdaptiveCardSession2 m_correctiveActionCardSession;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ApplyConfigurationActionRequiredEventArgs : ApplyConfigurationActionRequiredEventArgsT<ApplyConfigurationActionRequiredEventArgs, implementation::ApplyConfigurationActionRequiredEventArgs>
    {
    };
}

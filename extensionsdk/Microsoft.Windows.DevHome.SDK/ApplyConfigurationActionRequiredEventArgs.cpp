#include "pch.h"
#include "ApplyConfigurationActionRequiredEventArgs.h"
#include "ApplyConfigurationActionRequiredEventArgs.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ApplyConfigurationActionRequiredEventArgs::ApplyConfigurationActionRequiredEventArgs(IExtensionAdaptiveCardSession2 const& correctiveActionCardSession) :
        m_correctiveActionCardSession(correctiveActionCardSession)
    {
    }

    IExtensionAdaptiveCardSession2 ApplyConfigurationActionRequiredEventArgs::CorrectiveActionCardSession()
    {
        return m_correctiveActionCardSession;
    }
}

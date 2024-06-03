#include "pch.h"
#include "CreateComputeSystemActionRequiredEventArgs.h"
#include "CreateComputeSystemActionRequiredEventArgs.g.cpp"


namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    CreateComputeSystemActionRequiredEventArgs::CreateComputeSystemActionRequiredEventArgs(IExtensionAdaptiveCardSession2 const& correctiveActionCardSession) :
        m_correctiveActionCardSession(correctiveActionCardSession)
    {
    }
    
    IExtensionAdaptiveCardSession2 CreateComputeSystemActionRequiredEventArgs::CorrectiveActionCardSession()
    {
        return m_correctiveActionCardSession;
    }
}

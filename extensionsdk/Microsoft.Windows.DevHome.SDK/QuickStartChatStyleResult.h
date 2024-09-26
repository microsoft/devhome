#pragma once
#include "QuickStartChatStyleResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct QuickStartChatStyleResult : QuickStartChatStyleResultT<QuickStartChatStyleResult>
    {
        QuickStartChatStyleResult() = default;
        
        QuickStartChatStyleResult(hstring const& chatResponse);
        hstring ChatResponse();

    private:
        hstring const _ChatResponse;
    };
}

namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct QuickStartChatStyleResult : QuickStartChatStyleResultT<QuickStartChatStyleResult, implementation::QuickStartChatStyleResult>
    {
    };
}

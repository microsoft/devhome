#include "pch.h"
#include "QuickStartChatStyleResult.h"
#include "QuickStartChatStyleResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    QuickStartChatStyleResult::QuickStartChatStyleResult(hstring const& chatResponse) :
        _ChatResponse(chatResponse)
    {
    }

    hstring QuickStartChatStyleResult::ChatResponse()
    {
        return _ChatResponse;
    }
}

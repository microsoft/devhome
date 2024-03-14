// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "pch.h"

#include <mutex>

#include <QuietBackgroundProcesses.h>
#include "QuietState.h"

namespace QuietState
{
    std::mutex g_mutex;

    void TurnOff() noexcept
    {
        auto lock = std::scoped_lock(g_mutex);
        LOG_IF_FAILED(DisableQuietBackgroundProcesses());
    }

    unique_quietwindowclose_call TurnOn()
    {
        auto lock = std::scoped_lock(g_mutex);
        THROW_IF_FAILED(EnableQuietBackgroundProcesses());
        return unique_quietwindowclose_call{};
    }
}

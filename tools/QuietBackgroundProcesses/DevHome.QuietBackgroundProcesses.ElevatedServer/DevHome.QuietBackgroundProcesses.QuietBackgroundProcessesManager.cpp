// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

#include "pch.h"

#include <atomic>
#include <chrono>
#include <functional>
#include <future>
#include <memory>
#include <mutex>
#include <optional>
#include <vector>

#include <wil/resource.h>
#include <wil/win32_helpers.h>
#include <wil/result_macros.h>

#include "Timer.h"
#include "QuietState.h"
#include "DevHome.QuietBackgroundProcesses.QuietBackgroundProcessesManager.h"
#include "QuietBackgroundProcessesManager.g.cpp"

constexpr auto QUIET_DURATION = std::chrono::hours(2);

std::mutex g_mutex;
std::unique_ptr<Timer> g_activeTimer;

QuietState::unique_quietwindowclose_call g_quietState{false};

namespace winrt::DevHome::QuietBackgroundProcesses::implementation
{
    int64_t QuietBackgroundProcessesManager::Start()
    {
        auto lock = std::scoped_lock(g_mutex);

        // Discard the active timer
        Timer::Discard(std::move(g_activeTimer));

        // Start timer
        g_activeTimer.reset(new Timer(QUIET_DURATION, []()
        {
            auto lock = std::scoped_lock(g_mutex);
            g_quietState.reset();
        }));

        // Turn on quiet mode
        g_quietState = QuietState::TurnOn();

        // Return duration for showing countdown
        return g_activeTimer->TimeLeftInSeconds();
    }

    void QuietBackgroundProcessesManager::Stop()
    {
        auto lock = std::scoped_lock(g_mutex);

        // Turn off quiet mode
        g_quietState.reset();

        // Detach and destruct the current time window
        Timer::Discard(std::move(g_activeTimer));
    }

    bool QuietBackgroundProcessesManager::IsActive()
    {
        auto lock = std::scoped_lock(g_mutex);
        return (bool)g_quietState;
    }

    int64_t QuietBackgroundProcessesManager::TimeLeftInSeconds()
    {
        auto lock = std::scoped_lock(g_mutex);
        if (!g_quietState || !g_activeTimer)
        {
            return 0;
        }
        return g_activeTimer->TimeLeftInSeconds();
    }
}

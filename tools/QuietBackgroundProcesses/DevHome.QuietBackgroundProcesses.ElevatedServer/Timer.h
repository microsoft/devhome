// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

#include "pch.h"

#include <algorithm>
#include <atomic>
#include <chrono>
#include <functional>
#include <future>
#include <mutex>
#include <optional>

using CallbackFunction = void (*)();

class Timer
{
public:
    // Cleanup functions
    static void Discard(std::unique_ptr<Timer> timer);
    static void WaitForAllDiscardedTimersToDestruct();

    Timer(std::chrono::seconds seconds, CallbackFunction callback)
    {
        m_startTime = std::chrono::steady_clock::now();
        m_duration = seconds;
        m_callback = std::move(callback);
        m_timerThreadFuture = std::async(std::launch::async, &Timer::TimerThread, this);
    }

    Timer(Timer&& other) noexcept = default;
    Timer& operator=(Timer&& other) noexcept = default;

    Timer(const Timer&) = delete;
    Timer& operator=(const Timer&) = delete;

    void Cancel()
    {
        auto lock = std::scoped_lock(m_mutex);
        m_cancelled = true;
    }

    int64_t TimeLeftInSeconds()
    {
        auto lock = std::scoped_lock(m_mutex);
        if (m_cancelled)
        {
            return 0;
        }
        auto now = std::chrono::steady_clock::now();
        auto elapsed = std::chrono::duration_cast<std::chrono::seconds>(now - m_startTime);

        auto left = m_duration.count() - elapsed.count();
        return std::max(left, 0ll);
    }

private:
    void TimerThread()
    {
        // Pause until timer expired or cancelled
        while (true)
        {
            auto now = std::chrono::steady_clock::now();
            auto elapsed = std::chrono::duration_cast<std::chrono::seconds>(now - this->m_startTime);

            if (this->m_cancelled || elapsed >= m_duration)
            {
                break;
            }

            // Sleep for a short duration to avoid busy waiting
            std::this_thread::sleep_for(std::chrono::seconds(30));
        }

        // Do the callback
        auto lock = std::scoped_lock(m_mutex);
        if (!this->m_cancelled)
        {
            this->m_callback();
        }
    }

    std::chrono::steady_clock::time_point m_startTime{};
    std::chrono::seconds m_duration{};
    std::future<void> m_timerThreadFuture;
    std::mutex m_mutex;
    std::atomic<bool> m_cancelled{};
    CallbackFunction m_callback;
};

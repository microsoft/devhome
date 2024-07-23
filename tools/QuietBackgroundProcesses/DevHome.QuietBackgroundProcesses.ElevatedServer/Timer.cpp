// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

#include "pch.h"
#include "Timer.h"

std::mutex g_discardMutex;
std::thread g_discardThread;

void Timer::WaitForAllDiscardedTimersToDestruct()
{
    auto lock = std::scoped_lock(g_discardMutex);
    if (g_discardThread.joinable())
    {
        g_discardThread.join();
    }
}

void Timer::Discard(std::unique_ptr<Timer> timer)
{
    if (!timer)
    {
        return;
    }
    timer->Cancel();

    std::thread previousThread;
    {
        auto lock = std::scoped_lock(g_discardMutex);
        previousThread = std::move(g_discardThread);
    }

    // Destruct time window on sepearate thread because its destructor may take time to end (the std::future member is blocking)
    // 
    // (Make a new discard thread and chain the existing one to it)
    g_discardThread = std::thread([timer = std::move(timer), previousThread = std::move(previousThread)]() mutable {
        // Delete the timer (blocking)
        timer.reset();

        // Finish previous discard thread if there was one
        if (previousThread.joinable())
        {
            previousThread.join();
        }
    });
}

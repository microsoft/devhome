// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

#pragma once
#include "QuietBackgroundProcessesManager.g.h"

namespace winrt::DevHome::QuietBackgroundProcesses::implementation
{
    struct QuietBackgroundProcessesManager : QuietBackgroundProcessesManagerT<QuietBackgroundProcessesManager>
    {
        QuietBackgroundProcessesManager() = default;

        static int64_t Start();
        static void Stop();
        static bool IsActive();
        static int64_t TimeLeftInSeconds();
    };
}
namespace winrt::DevHome::QuietBackgroundProcesses::factory_implementation
{
    struct QuietBackgroundProcessesManager : QuietBackgroundProcessesManagerT<QuietBackgroundProcessesManager, implementation::QuietBackgroundProcessesManager>
    {
    };
}

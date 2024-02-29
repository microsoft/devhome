// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

#pragma once

#include <wil/resource.h>

namespace QuietState
{
    void TurnOff() noexcept;

    using unique_quietwindowclose_call = wil::unique_call<decltype(&TurnOff), TurnOff>;
    
    unique_quietwindowclose_call TurnOn();
}


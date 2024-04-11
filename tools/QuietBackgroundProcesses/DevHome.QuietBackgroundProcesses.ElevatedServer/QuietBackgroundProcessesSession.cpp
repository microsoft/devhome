// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <pch.h>

#include <chrono>
#include <memory>
#include <mutex>

#include <wrl/client.h>
#include <wrl/implements.h>
#include <wrl/module.h>

#include <wil/resource.h>
#include <wil/result_macros.h>
#include <wil/win32_helpers.h>
#include <wil/winrt.h>

#include "TimedQuietSession.h"

#include "DevHome.QuietBackgroundProcesses.h"
#include "PerformanceRecorderEngineWinrt.h"

constexpr auto DEFAULT_QUIET_DURATION = std::chrono::hours(2);

std::mutex g_mutex;
std::unique_ptr<TimedQuietSession> g_activeTimer;
wil::com_ptr<ABI::DevHome::QuietBackgroundProcesses::IPerformanceRecorderEngine> g_performanceRecorderEngine;

namespace ABI::DevHome::QuietBackgroundProcesses
{
    class QuietBackgroundProcessesSession :
        public Microsoft::WRL::RuntimeClass<
            Microsoft::WRL::RuntimeClassFlags<Microsoft::WRL::WinRt>,
            IQuietBackgroundProcessesSession,
            Microsoft::WRL::FtmBase>
    {
        InspectableClass(RuntimeClass_DevHome_QuietBackgroundProcesses_QuietBackgroundProcessesSession, BaseTrust);

    public:
        STDMETHODIMP RuntimeClassInitialize() noexcept
        {
            return S_OK;
        }

        // IQuietBackgroundProcessesSession
        STDMETHODIMP Start(__int64* result) noexcept override try
        {
            auto lock = std::scoped_lock(g_mutex);

            // Stop and discard the previous timer
            if (g_activeTimer)
            {
                g_activeTimer->Cancel();
            }

            std::chrono::seconds duration = DEFAULT_QUIET_DURATION;
            if (auto durationOverride = try_get_registry_value_dword(HKEY_LOCAL_MACHINE, LR"(Software\Microsoft\Windows\CurrentVersion\DevHome\QuietBackgroundProcesses)", L"Duration"))
            {
                duration = std::chrono::seconds(durationOverride.value());
            }

            // Start timer
            g_activeTimer.reset(new TimedQuietSession(duration));

            // Start performance recorder
            g_performanceRecorderEngine.reset();
            THROW_IF_FAILED(Microsoft::WRL::MakeAndInitialize<PerformanceRecorderEngine>(&g_performanceRecorderEngine));
            ABI::Windows::Foundation::TimeSpan samplingPeriod;
            samplingPeriod.Duration = 1000 * 10000; // 1 second
            THROW_IF_FAILED(g_performanceRecorderEngine->Start(samplingPeriod));

            // Return duration for showing countdown
            *result = g_activeTimer->TimeLeftInSeconds();
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP Stop(ABI::DevHome::QuietBackgroundProcesses::IProcessPerformanceTable** result) noexcept override
        try
        {
            auto lock = std::scoped_lock(g_mutex);
            *result = nullptr;

            if (g_performanceRecorderEngine)
            {
                THROW_IF_FAILED(g_performanceRecorderEngine->Stop(result));
                g_performanceRecorderEngine.reset();
            }

            // Turn off quiet mode and cancel timer
            if (g_activeTimer)
            {
                g_activeTimer->Cancel();
                g_activeTimer.reset();
            }

            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP get_IsActive(::boolean* value) noexcept override try
        {
            auto lock = std::scoped_lock(g_mutex);
            *value = false;
            if (g_activeTimer)
            {
                *value = g_activeTimer->IsActive();
            }
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP get_TimeLeftInSeconds(__int64* value) noexcept override try
        {
            auto lock = std::scoped_lock(g_mutex);
            *value = 0;
            if (g_activeTimer)
            {
                *value = g_activeTimer->TimeLeftInSeconds();
            }
            return S_OK;
        }
        CATCH_RETURN()
    };

    class QuietBackgroundProcessesSessionStatics WrlFinal :
        public Microsoft::WRL::AgileActivationFactory<
            Microsoft::WRL::Implements<IQuietBackgroundProcessesSessionStatics>>
    {
        InspectableClassStatic(RuntimeClass_DevHome_QuietBackgroundProcesses_QuietBackgroundProcessesSession, BaseTrust);

    public:
        STDMETHODIMP ActivateInstance(_COM_Outptr_ IInspectable**) noexcept
        {
            // Disallow activation - must use GetSingleton()
            return E_NOTIMPL;
        }

        // IQuietBackgroundProcessesSessionStatics
        STDMETHODIMP GetSingleton(_COM_Outptr_ IQuietBackgroundProcessesSession** session) noexcept override try
        {
            // Instanced objects are the only feasible way to manage a COM singleton without keeping a strong
            // handle to the server - which keeps it alive.  (IWeakReference keeps a strong handle to the server!)
            // An 'instance' can be thought of as a 'handle' to 'the singleton' backend.
            THROW_IF_FAILED(Microsoft::WRL::MakeAndInitialize<QuietBackgroundProcessesSession>(session));
            return S_OK;
        }
        CATCH_RETURN()
    };

    ActivatableClassWithFactory(QuietBackgroundProcessesSession, QuietBackgroundProcessesSessionStatics);
}

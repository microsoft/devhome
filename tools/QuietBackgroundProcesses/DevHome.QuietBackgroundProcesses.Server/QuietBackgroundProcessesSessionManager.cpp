// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <pch.h>

#include <mutex>

#include <wrl/client.h>
#include <wrl/wrappers/corewrappers.h>
#include <wrl/implements.h>
#include <wrl/module.h>
#include <wil/winrt.h>

#include <QuietBackgroundProcesses.h>

#include "DevHome.QuietBackgroundProcesses.QuietBackgroundProcessesSession.h"
#include "DevHome.QuietBackgroundProcesses.QuietBackgroundProcessesSessionManager.h"

#define STATUS_ACCESS_DENIED ((NTSTATUS)0xC0000022L)
#define STATUS_OBJECT_NAME_NOT_FOUND ((NTSTATUS)0xC0000034L)

static bool isFeatureSupported() noexcept
{
    HRESULT hr = DisableQuietBackgroundProcesses();
    if (hr == HRESULT_FROM_NT(STATUS_OBJECT_NAME_NOT_FOUND))
    {
        return false;
    }
    else if (hr == HRESULT_FROM_NT(STATUS_ACCESS_DENIED))
    {
        return true;
    }
    LOG_HR(hr);
    return false;
}

namespace ABI::DevHome::QuietBackgroundProcesses
{
    class QuietBackgroundProcessesSessionManager :
        public Microsoft::WRL::RuntimeClass<
            Microsoft::WRL::RuntimeClassFlags<Microsoft::WRL::WinRt>,
            IQuietBackgroundProcessesSessionManager,
            Microsoft::WRL::FtmBase>
    {
        InspectableClass(RuntimeClass_DevHome_QuietBackgroundProcesses_QuietBackgroundProcessesSessionManager, BaseTrust);

    public:
        STDMETHODIMP RuntimeClassInitialize() noexcept
        {
            return S_OK;
        }
    };

    class QuietBackgroundProcessesSessionManagerStatics WrlFinal :
        public Microsoft::WRL::AgileActivationFactory<
            Microsoft::WRL::Implements<IQuietBackgroundProcessesSessionManagerStatics>>
    {
        InspectableClassStatic(RuntimeClass_DevHome_QuietBackgroundProcesses_QuietBackgroundProcessesSessionManager, BaseTrust);

    public:
        // IActivationFactory method
        STDMETHODIMP ActivateInstance(_Outptr_result_nullonfailure_ IInspectable** ppvObject) noexcept
        try
        {
            THROW_IF_FAILED(Microsoft::WRL::MakeAndInitialize<QuietBackgroundProcessesSessionManager>(ppvObject));
            return S_OK;
        }
        CATCH_RETURN()

        // IQuietBackgroundProcessesSessionManagerStatics
        STDMETHODIMP IsFeatureSupported(_Out_ boolean* isSupported) noexcept override
        {
            *isSupported = isFeatureSupported();
            return S_OK;
        }

        STDMETHODIMP GetSession(_Outptr_result_nullonfailure_ IQuietBackgroundProcessesSession** session) noexcept override
        try
        {
            auto lock = std::scoped_lock(m_mutex);
            *session = nullptr;

            if (!m_sessionReference)
            {
                auto factory = wil::GetActivationFactory<IQuietBackgroundProcessesSessionStatics>(RuntimeClass_DevHome_QuietBackgroundProcesses_QuietBackgroundProcessesSession);
                THROW_IF_FAILED(factory->GetSingleton(&m_sessionReference));
            }
            m_sessionReference.copy_to(session);
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP TryGetSession(_COM_Outptr_result_maybenull_ IQuietBackgroundProcessesSession** session) noexcept override try
        {
            auto lock = std::scoped_lock(m_mutex);
            m_sessionReference.try_copy_to(session);
            return S_OK;
        }
        CATCH_RETURN()

    private:
        std::mutex m_mutex;
        wil::com_ptr<IQuietBackgroundProcessesSession> m_sessionReference;
    };

    ActivatableClassWithFactory(QuietBackgroundProcessesSessionManager, QuietBackgroundProcessesSessionManagerStatics);
}

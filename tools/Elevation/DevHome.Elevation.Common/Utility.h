// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include <string>

#include <wrl/client.h>
#include <wrl/wrappers/corewrappers.h>
#include <wrl/implements.h>
#include <wrl/module.h>

#include <wil/com.h>
#include <wil/registry.h>
#include <wil/resource.h>
#include <wil/result_macros.h>
#include <wil/token_helpers.h>
#include <wil/win32_helpers.h>
#include <wil/winrt.h>

#include <windows.h>
#include <shellapi.h>

inline std::optional<uint32_t> try_get_registry_value_dword(HKEY key, _In_opt_ PCWSTR subKey, _In_opt_ PCWSTR value_name, ::wil::reg::key_access access = ::wil::reg::key_access::read)
{
    wil::unique_hkey hkey;
    if (SUCCEEDED(wil::reg::open_unique_key_nothrow(key, subKey, hkey, access)))
    {
        if (auto keyvalue = wil::reg::try_get_value_dword(hkey.get(), value_name))
        {
            return keyvalue.value();
        }
    }
    return std::nullopt;
}

inline void WaitForDebuggerIfPresent()
{
    auto waitForDebugger = try_get_registry_value_dword(HKEY_LOCAL_MACHINE, LR"(Software\Microsoft\Windows\CurrentVersion\DevHome\QuietBackgroundProcesses)", L"WaitForDebugger");

    if (waitForDebugger.value_or(0))
    {
        while (!IsDebuggerPresent())
        {
            Sleep(1000);
        };
        DebugBreak();
    }
}

inline LONG GetTokenMandatoryLabel(HANDLE token)
{
    auto mandatoryLabel = wil::get_token_information<TOKEN_MANDATORY_LABEL>(token);
    LONG levelRid = static_cast<SID*>(mandatoryLabel->Label.Sid)->SubAuthority[0];
    return levelRid;
}

inline bool IsTokenElevated(HANDLE token)
{
    return GetTokenMandatoryLabel(token) == SECURITY_MANDATORY_HIGH_RID;
}

inline void SetComFastRundownAndNoEhHandle()
{
    // Enable fast rundown of COM stubs in this process to ensure that RPCSS bookkeeping is updated synchronously.
    wil::com_ptr<IGlobalOptions> pGlobalOptions;
    THROW_IF_FAILED(CoCreateInstance(CLSID_GlobalOptions, NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&pGlobalOptions)));
    THROW_IF_FAILED(pGlobalOptions->Set(COMGLB_RO_SETTINGS, COMGLB_FAST_RUNDOWN));
    THROW_IF_FAILED(pGlobalOptions->Set(COMGLB_EXCEPTION_HANDLING, COMGLB_EXCEPTION_DONOT_HANDLE_ANY));
}

MIDL_INTERFACE("68c6a1b9-de39-42c3-8d28-bf40a5126541")
ICallingProcessInfo : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE OpenCallerProcessHandle(
        DWORD desiredAccess,
        /* [annotation][out] */
        _Out_ HANDLE * callerPocessHandle) = 0;
};

inline wil::unique_handle GetCallingProcessHandle()
{
    Microsoft::WRL::ComPtr<ICallingProcessInfo> callingProcessInfo;
    THROW_IF_FAILED(CoGetCallContext(IID_PPV_ARGS(&callingProcessInfo)));

    wil::unique_handle callingProcessHandle;
    THROW_IF_FAILED(callingProcessInfo->OpenCallerProcessHandle(PROCESS_QUERY_LIMITED_INFORMATION, callingProcessHandle.addressof()));
    return callingProcessHandle;
}

inline DWORD GetCallingProcessPid()
{
    return GetProcessId(GetCallingProcessHandle().get());
}

inline DWORD GetCallingProcessMandatoryLabel()
{
    return GetTokenMandatoryLabel(GetCallingProcessHandle().get());
}

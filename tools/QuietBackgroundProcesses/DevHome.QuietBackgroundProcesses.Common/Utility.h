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

constexpr bool DEBUG_BUILD =
#if _DEBUG
    true;
#else
    false;
#endif

template <typename T>
struct wrl_module_object_ref
{
    struct details
    {
        static void wrl_decrement_object_count()
        {
            auto& module = T::GetModule();
            auto count = module.DecrementObjectCount();
            auto msg = std::wstring(L"WRL: DecrementObjectCount = ") + std::to_wstring(count) + std::wstring(L"\n");
            OutputDebugStringW(msg.c_str());
        }
    };

    using wrl_module_object_ref_releaser = wil::unique_call<decltype(&details::wrl_decrement_object_count), details::wrl_decrement_object_count>;

    wrl_module_object_ref()
    {
        auto& module = T::GetModule();
        auto count = module.IncrementObjectCount();
        auto msg = std::wstring(L"WRL: IncrementObjectCount = ") + std::to_wstring(count) + std::wstring(L"\n");
        OutputDebugStringW(msg.c_str());

        m_moduleReference.activate();
    }

    wrl_module_object_ref(wrl_module_object_ref&& other) noexcept = default;
    wrl_module_object_ref& operator=(wrl_module_object_ref&& other) noexcept = default;

    wrl_module_object_ref(const wrl_module_object_ref&) = delete;
    wrl_module_object_ref& operator=(const wrl_module_object_ref&) = delete;

    void reset()
    {
        m_moduleReference.reset();
    }

private:

    wrl_module_object_ref_releaser m_moduleReference{ false };
};

using wrl_server_process_ref = wrl_module_object_ref<Microsoft::WRL::Module<Microsoft::WRL::OutOfProc>>;

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

inline bool IsTokenElevated(HANDLE token)
{
    auto mandatoryLabel = wil::get_token_information<TOKEN_MANDATORY_LABEL>(token);
    LONG levelRid = static_cast<SID*>(mandatoryLabel->Label.Sid)->SubAuthority[0];
    return levelRid == SECURITY_MANDATORY_HIGH_RID;
}

inline void SelfElevate(std::optional<std::wstring> const& arguments)
{
    auto path = wil::GetModuleFileNameW();

    SHELLEXECUTEINFO sei = { sizeof(sei) };
    sei.lpVerb = L"runas";
    sei.lpFile = path.get();
    sei.lpParameters = arguments.value().c_str();
    sei.hwnd = NULL;
    sei.nShow = SW_NORMAL;

    THROW_LAST_ERROR_IF(!ShellExecuteEx(&sei));
}

inline std::wstring ParseServerNameArgument(std::wstring_view wargv)
{
    constexpr wchar_t serverNamePrefix[] = L"-ServerName:";
    if (_wcsnicmp(wargv.data(), serverNamePrefix, wcslen(serverNamePrefix)) != 0)
    {
        THROW_HR(E_UNEXPECTED);
    }
    return { wargv.data() + wcslen(serverNamePrefix) };
}

inline void SetComFastRundownAndNoEhHandle()
{
    // Enable fast rundown of COM stubs in this process to ensure that RPCSS bookkeeping is updated synchronously.
    wil::com_ptr<IGlobalOptions> pGlobalOptions;
    THROW_IF_FAILED(CoCreateInstance(CLSID_GlobalOptions, NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&pGlobalOptions)));
    THROW_IF_FAILED(pGlobalOptions->Set(COMGLB_RO_SETTINGS, COMGLB_FAST_RUNDOWN));
    THROW_IF_FAILED(pGlobalOptions->Set(COMGLB_EXCEPTION_HANDLING, COMGLB_EXCEPTION_DONOT_HANDLE_ANY));
}

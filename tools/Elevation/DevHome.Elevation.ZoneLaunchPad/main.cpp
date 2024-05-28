// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <pch.h>

#include <filesystem>
#include <memory>
#include <mutex>

#include <wrl/client.h>
#include <wrl/wrappers/corewrappers.h>
#include <wrl/implements.h>
#include <wrl/module.h>
#include <wil/com.h>
#include <wil/result_macros.h>
#include <wil/token_helpers.h>
#include <wil/win32_helpers.h>
#include <wil/winrt.h>

#include <objbase.h>
#include <roregistrationapi.h>
#include <shobjidl_core.h>


#include "DevHome.Elevation.h"
#include "Utility.h"

void CreateProcessW(std::filesystem::path const& path, std::optional<std::wstring> const& arguments, bool elevated = false)
{
    THROW_HR_IF(E_INVALIDARG, elevated);

    STARTUPINFO startupInfo{};
    startupInfo.cb = sizeof(startupInfo);
    wil::unique_process_information processInfo;

    PCWSTR lpwstrExePath{};
    LPWSTR lpwstrCommandLine{};
    std::wstring commandLine;
    if (arguments)
    {
        commandLine = path.wstring() + L" " + arguments.value();
        lpwstrCommandLine = const_cast<LPWSTR>(commandLine.c_str());
    }
    else
    {
        lpwstrExePath = path.c_str();
    }

    THROW_IF_WIN32_BOOL_FALSE(CreateProcess(lpwstrExePath, lpwstrCommandLine, nullptr, nullptr, TRUE, 0, nullptr, nullptr, &startupInfo, &processInfo));

    // Let process finish
    //wil::handle_wait(processInfo.hProcess);

    //auto pid = GetProcessId(processInfo.hProcess);
}

DWORD GetParentProcessId()
{
    ULONG_PTR pbi[6];
    ULONG ulSize = 0;
    LONG(WINAPI * NtQueryInformationProcess)
    (HANDLE ProcessHandle, ULONG ProcessInformationClass, PVOID ProcessInformation, ULONG ProcessInformationLength, PULONG ReturnLength);
    *(FARPROC*)&NtQueryInformationProcess =
        GetProcAddress(LoadLibraryA("NTDLL.DLL"), "NtQueryInformationProcess");
    if (NtQueryInformationProcess)
    {
        if (NtQueryInformationProcess(GetCurrentProcess(), 0, &pbi, sizeof(pbi), &ulSize) >= 0 && ulSize == sizeof(pbi))
            return static_cast<DWORD>(pbi[5]);
    }
    return (DWORD)-1;
}

int __stdcall wWinMain(HINSTANCE, HINSTANCE, LPWSTR, int wargc) try
{
    WaitForDebuggerIfPresent();

    if (wargc < 1)
    {
        THROW_HR(E_INVALIDARG);
    }

    /*
    // Parse the target Zone to be launched
    //auto zoneName = wargv[1];
    auto zoneName = wargv;
    auto pid = 123;
    auto zoneToLaunch = zoneName;

    // Split commandline arguments on space
    auto arguments = CommandLineToArgvW(GetCommandLineW(), &wargc);
    if (arguments)
    {
        zoneName = arguments[1];
        pid = std::stoi(arguments[2]);
        zoneToLaunch = arguments[3];
    }
    */

    
    //auto zoneName = ((wchar_t*)(__argc))[2];
    auto zoneName = __wargv[2];
    auto voucherName = __wargv[4];
    //auto pid = 123;
    auto zoneToLaunch = zoneName;

    //if (wil::compare_string_ordinal(zoneName, L"ZoneA", true) != 0)
    //{
        //THROW_HR(E_INVALIDARG);
    //}

    // Make sure we're already elevated instance
    if (!IsTokenElevated(GetCurrentProcessToken()))
    {
        THROW_HR(E_INVALIDARG);
    }

    //WaitForDebuggerIfPresent();

    auto unique_rouninitialize_call = wil::RoInitialize();


    // Get PID of parent process
    auto parentProcessId = GetParentProcessId();

    // Get parent process create time
    auto process = wil::unique_process_handle{ OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, parentProcessId) };
    THROW_IF_NULL_ALLOC(process.get());

    FILETIME createTime{};
    FILETIME exitTime{};
    FILETIME kernelTime{};
    FILETIME userTime{};
    THROW_IF_WIN32_BOOL_FALSE(GetProcessTimes(process.get(), &createTime, &exitTime, &kernelTime, &userTime));

    INT64 createTime64 = createTime.dwLowDateTime + ((UINT64)createTime.dwHighDateTime << 32);
    auto createTimeDatetime = ABI::Windows::Foundation::DateTime{ createTime64 };


    auto eventName = std::wstring{} + L"Global\\DevHome_Elevation_Server__Started";
    wil::unique_event elevatedServerRunningEvent22;
    elevatedServerRunningEvent22.create(wil::EventOptions::ManualReset, eventName.c_str());

    {
        // CreateProcess
        //CreateProcessW(L"DevHome.Elevation.Server.exe", L"-ServerName:DevHome.Elevation.Server", false);
        auto activationManager = wil::CoCreateInstance<ApplicationActivationManager, IApplicationActivationManager>();


        DWORD processId = 0;
        THROW_IF_FAILED(activationManager->ActivateApplication(L"Microsoft.Windows.DevHome.Dev_8wekyb3d8bbwe!AppElevationServer", nullptr, AO_NOERRORUI, &processId));

        {
            auto eventName234 = std::wstring{} + L"Global\\DevHome_Elevation_Server__Started";
            wil::unique_event elevatedServerRunningEvent;
            elevatedServerRunningEvent.create(wil::EventOptions::ManualReset, eventName234.c_str());
        }
    }

    elevatedServerRunningEvent22.wait();

    //zoneConnectionManager->LaunchZone(zoneName);
    //wil::CoCreateInstance<ABI::DevHome::Elevation::ZoneConnectionManager>(CLSID_ZoneA, CLSCTX_LOCAL_SERVER);
    //auto zoneConnectionManager = wil::GetActivationFactory<ABI::DevHome::Elevation::IZoneConnectionManagerStatics>(L"DevHome.Elevation.ZoneConnectionManager");

    auto voucherFactory = wil::GetActivationFactory<ABI::DevHome::Elevation::IElevationVoucherFactory>(RuntimeClass_DevHome_Elevation_ElevationVoucher);

    wil::com_ptr < ABI::DevHome::Elevation::IElevationVoucher> voucher;
    THROW_IF_FAILED(voucherFactory->CreateInstance(
        Microsoft::WRL::Wrappers::HStringReference(voucherName).Get(),
        ABI::DevHome::Elevation::ElevationLevel_High,
        ABI::DevHome::Elevation::ElevationZone_ElevationZoneA,
        parentProcessId,
        createTimeDatetime,
        &voucher));

    /*
    auto voucher = wil::ActivateInstance<ABI::DevHome::Elevation::IElevationVoucher>(
        RuntimeClass_DevHome_Elevation_ElevationVoucher,
        ABI::DevHome::Elevation::ElevationZone_ElevationZoneA,
        parentProcessId,
        createTimeDatetime);
    */

    auto voucherManager = wil::GetActivationFactory<ABI::DevHome::Elevation::IElevationVoucherManagerStatics>(RuntimeClass_DevHome_Elevation_ElevationVoucherManager);

    ABI::Windows::Foundation::TimeSpan validDuration;
    validDuration.Duration = 1000 * 10000 * 11; // 11 seconds

    THROW_IF_FAILED(voucherManager->ActivateVoucher(voucher.get(), validDuration));

    return 0;
}
CATCH_RETURN()

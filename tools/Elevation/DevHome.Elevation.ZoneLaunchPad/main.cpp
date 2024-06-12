// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <pch.h>

#include <wil/com.h>
#include <wil/result_macros.h>

#include <shobjidl_core.h>

#include "DevHome.Elevation.h"
#include "Utility.h"

DWORD GetParentProcessId()
{
    ULONG_PTR pbi[6];
    ULONG ulSize = 0;
    LONG(WINAPI * NtQueryInformationProcess)
    (HANDLE ProcessHandle, ULONG ProcessInformationClass, PVOID ProcessInformation, ULONG ProcessInformationLength, PULONG ReturnLength);
    *(FARPROC*)&NtQueryInformationProcess = GetProcAddress(LoadLibraryA("NTDLL.DLL"), "NtQueryInformationProcess");
    if (NtQueryInformationProcess)
    {
        if (NtQueryInformationProcess(GetCurrentProcess(), 0, &pbi, sizeof(pbi), &ulSize) >= 0 && ulSize == sizeof(pbi))
            return static_cast<DWORD>(pbi[5]);
    }
    return (DWORD)-1;
}

ABI::DevHome::Elevation::ElevationZone GetZoneEnumFromName(const std::wstring& zoneName)
{
    if (zoneName == L"ElevationZoneA")
    {
        return ABI::DevHome::Elevation::ElevationZone::ElevationZoneA;
    }
    THROW_HR(E_INVALIDARG);
}

int __stdcall wWinMain(HINSTANCE, HINSTANCE, LPWSTR, int wargc) try
{
    WaitForDebuggerIfPresent();

    // Make sure we're already elevated instance
    if (!IsTokenElevated(GetCurrentProcessToken()))
    {
        THROW_HR(E_INVALIDARG);
    }

    if (wargc < 1)
    {
        THROW_HR(E_INVALIDARG);
    }

    // Get the arguments (todo: improve this)
    auto zoneToLaunch = __wargv[2];
    auto voucherName = __wargv[4];

    auto zone = GetZoneEnumFromName(zoneToLaunch);
    
    auto unique_rouninitialize_call = wil::RoInitialize();

    // Wake up the server with our elevated token (and packaged identity) so it can register factories
    {
        wil::unique_event elevatedServerRunningEvent;
        elevatedServerRunningEvent.create(wil::EventOptions::ManualReset, L"Global\\DevHome_Elevation_Server__Started");

        auto activationManager = wil::CoCreateInstance<ApplicationActivationManager, IApplicationActivationManager>();
        DWORD processId = 0;
        THROW_IF_FAILED(activationManager->ActivateApplication(L"Microsoft.Windows.DevHome.Dev_8wekyb3d8bbwe!AppElevationServer", nullptr, AO_NOERRORUI, &processId));

        // todo: make sure the server is single instanced and prevent this event-wait dance if already running
        elevatedServerRunningEvent.wait();
    }

    // Get PID of parent process
    auto parentProcessId = GetParentProcessId();

    // Create voucher
    auto voucherFactory = wil::GetActivationFactory<ABI::DevHome::Elevation::IElevationVoucherFactory>(RuntimeClass_DevHome_Elevation_ElevationVoucher);

    wil::com_ptr < ABI::DevHome::Elevation::IElevationVoucher> voucher;
    THROW_IF_FAILED(voucherFactory->CreateInstance(
        Microsoft::WRL::Wrappers::HStringReference(voucherName).Get(),
        ABI::DevHome::Elevation::ElevationLevel_High,
        zone,
        parentProcessId,
        &voucher));

    // Add voucher to will-call
    auto voucherManager = wil::GetActivationFactory<ABI::DevHome::Elevation::IElevationVoucherManagerStatics>(RuntimeClass_DevHome_Elevation_ElevationVoucherManager);
    THROW_IF_FAILED(voucherManager->AddVoucherToWillCall(voucher.get()));

    return 0;
}
CATCH_RETURN()

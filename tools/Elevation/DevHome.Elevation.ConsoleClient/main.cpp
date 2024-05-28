// DevHome.Elevation.ConsoleClient.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <filesystem>
#include <iostream>

#include <wrl/client.h>
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

#include "DevHome.Elevation.h"

int main() try
{
    auto unique_rouninitialize_call = wil::RoInitialize();

    auto zoneToLaunch = std::wstring(L"ElevationZoneA");
    auto voucherName = std::wstring(L"abc");

    std::cout << "Create path..." << std::endl;

    // Launch elevated ZoneLaunchPad instance
    auto pathToZoneLaunchPad = std::filesystem::path(wil::GetModuleFileNameW().get());
    pathToZoneLaunchPad = pathToZoneLaunchPad.replace_filename(L"DevHome.Elevation.ZoneLaunchPad.exe");

    auto pathString = pathToZoneLaunchPad.wstring();
    auto arguments = std::wstring{} + L"-ElevationZone " + zoneToLaunch + L" -VoucherName " + voucherName;

    SHELLEXECUTEINFO sei = { sizeof(sei) };
    sei.lpVerb = L"runas";
    sei.lpFile = pathString.c_str();
    sei.lpParameters = arguments.c_str();
    sei.fMask = SEE_MASK_FLAG_NO_UI | SEE_MASK_NOCLOSEPROCESS | SEE_MASK_NOASYNC;
    sei.hwnd = NULL;
    sei.nShow = SW_NORMAL;

    std::cout << "ShellExecute..." << std::endl;
    THROW_LAST_ERROR_IF(!ShellExecuteEx(&sei));
    wil::unique_handle process(sei.hProcess);

    std::cout << "Wait..." << std::endl;
    wil::handle_wait(process.get());

    std::cout << "GetExitCodeProcess..." << std::endl;
    DWORD exitCode = ERROR_SUCCESS;
    THROW_IF_WIN32_BOOL_FALSE(GetExitCodeProcess(process.get(), &exitCode));
    std::cout << "Exit code = " << std::hex << exitCode << std::endl;

    // Create voucher manager
    auto voucherManager = wil::GetActivationFactory<ABI::DevHome::Elevation::IElevationVoucherManagerStatics>(RuntimeClass_DevHome_Elevation_ElevationVoucherManager);

    // Claim voucher
    wil::com_ptr<ABI::DevHome::Elevation::IElevationVoucher> elevationVoucher;
    THROW_IF_FAILED(voucherManager->ClaimVoucher(Microsoft::WRL::Wrappers::HStringReference(voucherName.c_str()).Get(), &elevationVoucher));

    wil::com_ptr<ABI::DevHome::Elevation::IElevationZone> elevationZone;
    THROW_IF_FAILED(elevationVoucher->Redeem(&elevationZone));

    auto elevationZoneA = elevationZone.query<ABI::DevHome::Elevation::Zones::IElevationZoneA>();

    unsigned int something;
    THROW_IF_FAILED(elevationZoneA->GetSomething(&something));
    std::cout << "Something = " << something << std::endl;

    return 0;
}
catch (...)
{
    std::cout << "exception = " << wil::ResultFromCaughtException() << std::endl;
    Sleep(2000);
}

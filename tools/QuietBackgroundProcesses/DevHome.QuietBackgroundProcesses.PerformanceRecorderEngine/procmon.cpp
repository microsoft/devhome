#include "pch.h"

#include <windows.h>
#include <wil/token_helpers.h>
#include <psapi.h>
#include <iostream>
#include <string>

BOOL SetPrivilege(
    HANDLE hToken, // access token handle
    LPCTSTR lpszPrivilege, // name of privilege to enable/disable
    BOOL bEnablePrivilege // to enable or disable privilege
);

extern "C" __declspec(dllexport) double GetProcessCpuUsage(DWORD processId)
{
    auto token = wil::open_current_access_token(TOKEN_ADJUST_PRIVILEGES);
    SetPrivilege(token.get(), L"SeDebugPrivilege", TRUE);

    HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, processId);

    if (hProcess == NULL)
    {
        std::cerr << "Failed to open process. Error code: " << GetLastError() << std::endl;
        return -1.0;
    }

    FILETIME createTime, exitTime, kernelTime, userTime;

    if (GetProcessTimes(hProcess, &createTime, &exitTime, &kernelTime, &userTime) == 0)
    {
        std::cerr << "Failed to get process times. Error code: " << GetLastError() << std::endl;
        CloseHandle(hProcess);
        return -1.0;
    }

    ULARGE_INTEGER totalTime;
    totalTime.LowPart = userTime.dwLowDateTime;
    totalTime.HighPart = userTime.dwHighDateTime;

    totalTime.LowPart += kernelTime.dwLowDateTime;
    totalTime.HighPart += kernelTime.dwHighDateTime;

    double cpuUsage = (totalTime.QuadPart * 100.0) / (GetTickCount64() * 10000.0);

    CloseHandle(hProcess);

    return cpuUsage;
}

void MonitorCpuUsage()
{
    DWORD processes[1024];
    DWORD needed;
    if (EnumProcesses(processes, sizeof(processes), &needed) == 0)
    {
        std::cerr << "Failed to enumerate processes. Error code: " << GetLastError() << std::endl;
        return;
    }

    int numProcesses = needed / sizeof(DWORD);

    for (int i = 0; i < numProcesses; i++)
    {
        double cpuUsage = GetProcessCpuUsage(processes[i]);
        if (cpuUsage >= 0.0)
        {
            std::cout << "Process ID: " << processes[i] << " - CPU Usage: " << cpuUsage << "%" << std::endl;
        }
    }
}

// stl
#include <algorithm>
#include <condition_variable>
#include <chrono>
#include <future>
#include <mutex>
#include <iostream>
#include <iterator>
#include <map>
#include <memory>
#include <optional>
#include <set>
#include <sstream>
#include <string>
#include <string_view>
#include <thread>
#include <tuple>
#include <unordered_set>
#include <vector>

// windows
//#include <nt.h>
//#/include <ntrtl.h>
//#include <nturtl.h>
//#include <ntstatus.h>


//#include <ntpsapi.h>
///WINAPI_PARTITION_DESKTOP
//#define WINAPI_PARTITION_DESKTOP 1
#include <winternl.h>
#include <windows.h>

// wil
#include <wil/com.h>
#include <wrl/implements.h>
#include <wil/resource.h>
#include <wil/result.h>
#include <wil/result_macros.h>
#include <wil/token_helpers.h>
#include <wil/win32_helpers.h>

// other
#include <shellapi.h>
#include "windows.h"
// #include <ActivityCoordinator.h>
#include "werapi.h"

/*
#include "winnls.h"
#include "shobjidl.h"
#include "objbase.h"
#include "objidl.h"
#include "shlguid.h"
#include "propkey.h"
#include "propkeyp.h"

// crm
#include <resourcemanagercrm.h>

// wnf stuff
#include "power.h"
#include "wnfnamesp.h"

// power stuff
#include "ntpoapi_p.h"
#include "powrprof.h" // DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS

// power saver (RM_QUIET_MODE_DATA)
#include "crmtypes.h"

// process stuff
#include <tlhelp32.h>

// battery stuff
#include "poclass.h"
*/

// process stuff
#include <tlhelp32.h>

#include "a.h"

namespace performance
{
    #define NEXT_PROCESS(p) reinterpret_cast<PSYSTEM_PROCESS_INFORMATION>(((PUCHAR)p + p->NextEntryOffset))

    double
    CalcProcessor(
        _In_ ULONGLONG const previousValue,
        _In_ ULONGLONG const currentValue,
        _In_ ULONGLONG const duration,
        _In_ USHORT numberOfCpus)
    {
        if (currentValue > previousValue && duration != 0)
        {
            double temp = (static_cast<double>((currentValue - previousValue) / static_cast<double>(duration))) * 100.0 / numberOfCpus;
            return temp > 100 ? 99.0 : temp;
        }

        return 0;
    }

    HRESULT
    GetCycleTime(_Out_ PULONGLONG timeStampCounter)
    {
        HRESULT hr = S_OK;
        THREAD_CYCLE_TIME_INFORMATION timeInfo;

        if (FAILED(hr = HRESULT_FROM_NT(NtQueryInformationThread(GetCurrentThread(), tic(ThreadCycleTime), &timeInfo, sizeof(timeInfo), NULL))))
        {
            return hr;
        }

        *timeStampCounter = timeInfo.CurrentCycleCount;
        return hr;
    }

    std::optional<SYSTEM_PROCESS_INFORMATION> GetProcessInfo(ULONG pid)
    {
        ULONG performanceInformationLength;
        SYSTEM_PERFORMANCE_INFORMATION performanceInformation;

        // general system performance metrics - not that useful
        THROW_IF_NTSTATUS_FAILED(NtQuerySystemInformation(SystemPerformanceInformation, &performanceInformation, sizeof(performanceInformation), &performanceInformationLength));

        // per-process info
        //SYSTEM_PROCESS_INFORMATION processInfos;
        //ULONG processInfosLength;
        ULONG cbSize = 0;
        NTSTATUS status{};
        //THROW_IF_NTSTATUS_FAILED(NtQuerySystemInformation(SystemProcessInformation, 0, sizeof(cbSize), &cbSize));
        //THROW_IF_NTSTATUS_FAILED

        status = NtQuerySystemInformation(SystemProcessInformation, 0, cbSize, &cbSize);
        //THROW_NTSTATUS(status);
        if (status != STATUS_INFO_LENGTH_MISMATCH)
        {
            THROW_NTSTATUS(status);
        }

        std::vector<BYTE> buffer;
        while (true)
        {
            buffer.resize(cbSize);
            // char* x = new char[cbSize];
            // THROW_IF_NTSTATUS_FAILED(NtQuerySystemInformation(SystemProcessInformation, x, cbSize, &cbSize));
            //THROW_IF_NTSTATUS_FAILED(NtQuerySystemInformation(SystemProcessInformation, buffer.data(), cbSize, &cbSize));
            //THROW_IF_NTSTATUS_FAILED(NtQuerySystemInformation(SystemProcessInformation, &buffer[0], cbSize, &cbSize));
            status = NtQuerySystemInformation(SystemProcessInformation, buffer.data(), cbSize, &cbSize);
            if (status == STATUS_INFO_LENGTH_MISMATCH)
            {
                continue;
            }
            THROW_IF_NTSTATUS_FAILED(status);
            break;
        }

        //log("cbSize ", cbSize);


        SYSTEM_PROCESS_INFORMATION* processInfoNext;
        SYSTEM_PROCESS_INFORMATION* processInfo;
        processInfoNext = reinterpret_cast<PSYSTEM_PROCESS_INFORMATION>((void *)buffer.data());
        do
        {
            processInfo = processInfoNext;
            THROW_HR_IF(E_FAIL, ((ULONG)((BYTE*)processInfo - (BYTE*)(void*)buffer.data()) > cbSize));

            auto currentPid = HandleToULong(processInfo->UniqueProcessId);

            if (currentPid == pid)
            {
                return {*processInfo};
            }

            //hr = this->Update(processInfo, duration, cycleDuration, index, &current, &cycles);
            processInfoNext = NEXT_PROCESS(processInfo);
        } while (processInfoNext != processInfo);

        return std::nullopt;
    }

    //std::vector<double> g_procUsage;
    //std::mutex g_procUsageLock;
    //std::atomic<double> g_procUsage;

    template <typename CallbackT>
    std::thread RunPerformanceMonitor(ULONG pid, CallbackT&& callback)
    {
        // Let's do this on another thread
        auto thread = std::thread([pid, callback = std::move(callback)]()
        {
            ULONGLONG thistimestamp{};
            ULONGLONG thissystemCycle{};

            // hrm
            ULONGLONG previousCycleTime = 0;
            ULONGLONG previousNewTime = 0;

            //double current = 0.0;
            //double cycles = 0.0;
            ULONGLONG timestampEnter;
            ULONGLONG timestampExit;
            ULONGLONG systemCycleEnter;
            ULONGLONG systemCycleExit;
            //SYSTEM_PROCESS_INFORMATION* processInfo;
            //SYSTEM_PROCESS_INFORMATION* processInfoNext;
            ULONGLONG cycleDuration = 0;
            ULONGLONG duration = 0;

            SYSTEM_INFO systemInfo;
            GetSystemInfo(&systemInfo);
            auto numCpus = (short)systemInfo.dwNumberOfProcessors;

            while (true)
            {
                QueryUnbiasedInterruptTime(&timestampEnter);
                performance::GetCycleTime(&systemCycleEnter);
                //nt = NtQuerySystemInformation(SystemProcessInformation, buffer, cbSize, &cbSize);
                //auto processInfo = performance::GetProcessInfo(pid);
                auto maybeProcessInfo = performance::GetProcessInfo(pid);
                if (!maybeProcessInfo)
                {
                    break;
                }
                
                auto processInfo = maybeProcessInfo.value();
                
                QueryUnbiasedInterruptTime(&timestampExit);
                performance::GetCycleTime(&systemCycleExit);

                if (thistimestamp != 0)
                {
                    duration = timestampExit - thistimestamp;
                }

                if (thissystemCycle != 0 && systemCycleExit != 0)
                {
                    cycleDuration = systemCycleExit - thissystemCycle;
                }

                thistimestamp = timestampEnter;
                thissystemCycle = systemCycleEnter;

                //log("process: ", pid);
                //log("processInfo->CycleTime: ", processInfo.CycleTime);

                ULONGLONG newTime = (ULONGLONG)(as_internal_type(processInfo).UserTime.QuadPart + as_internal_type(processInfo).KernelTime.QuadPart);

                

                double procUsage = performance::CalcProcessor(previousCycleTime, as_internal_type(processInfo).CycleTime, cycleDuration, numCpus);
                //double cputimeUsage = performance::CalcProcessor(previousNewTime, newTime, duration, 12);

                callback(procUsage);

                //log("cputimeUsage: ", cputimeUsage);
                previousCycleTime = as_internal_type(processInfo).CycleTime;
                previousNewTime = newTime;

                {
                    //std::scoped_lock<std::mutex> lock;
                    //g_procUsage.push_back(procUsage);
                    //g_procUsage = procUsage;
                }

                Sleep(500);
            }
            
            // Process ended, so send CPU usage as zero
            callback(0);
        });

        return thread;
    }
}

namespace performance
{

    unsigned long GetProcessPidByName(PCWSTR processName)
    {
        wil::unique_handle hSnapShot(CreateToolhelp32Snapshot(TH32CS_SNAPALL, NULL));
        THROW_LAST_ERROR_IF(hSnapShot.get() == INVALID_HANDLE_VALUE);
        PROCESSENTRY32 pEntry;
        pEntry.dwSize = sizeof(pEntry);
        BOOL hRes = Process32First(hSnapShot.get(), &pEntry);
        while (hRes)
        {
            if (wil::compare_string_ordinal(pEntry.szExeFile, processName, true) == 0)
            {
                return pEntry.th32ProcessID;
            }
            hRes = Process32Next(hSnapShot.get(), &pEntry);
        }
        return 0;
    }
}


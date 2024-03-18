#include "pch.h"

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
#include <set>
#include <sstream>
#include <string>
#include <string_view>
#include <thread>
#include <tuple>
#include <unordered_set>
#include <vector>

// windows
#include <windows.h>
// other
//#include <shlobj.h>
//#include <shlobj_core.h>

// wil
#include <wil/com.h>
#include <wrl/implements.h>
#include <wil/resource.h>
#include <wil/result.h>
#include <wil/result_macros.h>
#include <wil/token_helpers.h>
#include <wil/win32_helpers.h>

/*
// other
#include <shellapi.h>
#include "windows.h"
#include <ActivityCoordinator.h>
#include "werapi.h"

#include "winnls.h"
#include "shobjidl.h"
#include "objbase.h"
#include "objidl.h"
#include "shlguid.h"
#include "propkey.h"
#include "propkeyp.h"


// wnf stuff
#include "power.h"
#include "wnfnamesp.h"

// power stuff
#include "ntpoapi_p.h"

// power saver (RM_QUIET_MODE_DATA)
#include "crmtypes.h"

// battery stuff
#include "poclass.h"

// process stuff
#include <tlhelp32.h>
*/

// mine
//#include <..\inc\include_crm.h>
#include <..\inc\include_crm.h>


//
// Util
//
namespace details2
{
    template <typename Func, typename... Ts>
    void for_each_parameter_pack(Func f, Ts&&... args)
    {
        int ignored[] = { (f(std::forward<Ts&>(args)), 0)...};
        (void) ignored;
    }

    template <typename... Ts>
    std::wstring format_string(Ts&&... args)
    {
        std::wstringstream ss;
        for_each_parameter_pack([&ss](auto x){ ss << x; }, std::forward<Ts&>(args)...);
        return ss.str();
    }
}

template <typename... Ts>
std::wstring format(Ts&&... args)
{
    return details2::format_string(std::forward<Ts&>(args)...);
}

std::wstring join_string(std::vector<std::wstring> const & strings, std::wstring_view joinstring)
{
    std::wstring result;
    auto i = 0;
    for (const auto & string : strings)
    {
        if (i++ == 0)
        {
            result += string;
        }
        else
        {
            result += joinstring.data() + string;
        }
    }
    return result;
}

template <typename... Ts>
void jsprint(Ts&&... args)
{
    auto message = details2::format_string(std::forward<Ts&>(args)...);
    g_electronLogW(message.c_str());
}





//
// Exports
//

extern "C" ULONG __declspec(dllexport) PerformanceState_GetProcessPidByName(PCWSTR processName)
{
    return performance::GetProcessPidByName(processName);
}


/*
extern "C" bool __declspec(dllexport) PerformanceState_GetDeveloperQuietMode()
{
    return performance::GetDeveloperQuietMode();
}
*/

//std::atomic<double> g_cpuUsage;
//std::map<ULONG, double> g_cpuUsages;
//std::map<ULONG, std::tuple<std::thread, double>> g_cpuUsages;
std::map<ULONG, std::atomic<double>> g_cpuUsages;
extern "C" HRESULT __declspec(dllexport) PerformanceState_StartForProcess(ULONG pid)
{
    static std::mutex s_mutex;
    auto lock = std::scoped_lock<std::mutex>(s_mutex);
    auto thread = performance::RunPerformanceMonitor(pid, [pid](double cpuUsage)
    {
        //g_cpuUsage = cpuUsage;
        g_cpuUsages[pid] = cpuUsage;
    });
    thread.detach();
    return S_OK;
}

extern "C" ULONG __declspec(dllexport) PerformanceState_GetCpuUsageForProcess(ULONG pid)
{
    //return static_cast<ULONG>(g_cpuUsage);
    return static_cast<ULONG>(g_cpuUsages[pid]);
}

/*
extern "C" ULONG __declspec(dllexport) PerformanceState_GetBatteryInfo_AcOnLine()
{
    return std::get<1>(performance::GetBatteryInfo());
}
extern "C" bool __declspec(dllexport) PerformanceState_IsBatterySaverEnabled()
{
    return performance::IsBatterySaverEnabled();
}
*/

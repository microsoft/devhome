// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include <map>
#include <string_view>

#include <wil/resource.h>
#include <wil/token_helpers.h>
#include <wil/win32_helpers.h>

namespace ServiceInformation
{
    inline bool IsSvchost(std::wstring_view name)
    {
        return wil::compare_string_ordinal(name, L"svchost.exe", true) == 0;
    }

    inline std::map<ULONG, std::wstring> GetRunningServiceNames()
    {
        SC_HANDLE m_hScmManager = OpenSCManager(NULL, NULL, SC_MANAGER_ENUMERATE_SERVICE);
        THROW_LAST_ERROR_IF(m_hScmManager == NULL);

        std::unique_ptr<ENUM_SERVICE_STATUS_PROCESS> services{};
        ULONG servicesCount = 0;
        ULONG servicesSize = 0;

        while (true)
        {
            // Get services
            ULONG resumeIndex = 0;
            if (EnumServicesStatusEx(m_hScmManager, SC_ENUM_PROCESS_INFO, SERVICE_WIN32, SERVICE_STATE_ALL, (LPBYTE)services.get(), servicesSize, &servicesSize, &servicesCount, &resumeIndex, NULL))
            {
                break;
            }

            THROW_LAST_ERROR_IF(GetLastError() != ERROR_MORE_DATA);

            // Increase the buffer size and try again.
            servicesSize *= 2;
            services.reset(reinterpret_cast<ENUM_SERVICE_STATUS_PROCESS*>(new BYTE[servicesSize]));
            THROW_IF_NULL_ALLOC(services);
            ZeroMemory(services.get(), servicesSize);
        }

        auto map = std::map<ULONG, std::wstring>();
        for (ULONG index = 0; index < servicesCount; index++)
        {
            map.emplace(services.get()[index].ServiceStatusProcess.dwProcessId, services.get()[index].lpServiceName);
        }
        return map;
    }

    class RunningServiceInformation
    {
    public:
        std::optional<std::wstring> TryGetServiceName(DWORD pid, std::wstring_view name) noexcept try
        {
            // Check if we have the service name cached
            auto it = m_runningServiceNames.find(pid);
            if (it != m_runningServiceNames.end())
            {
                return it->second;
            }

            // If the process is svchost..
            if (IsSvchost(name))
            {
                // Update cache..
                m_runningServiceNames = GetRunningServiceNames();

                // ..and try again
                auto it = m_runningServiceNames.find(pid);
                if (it != m_runningServiceNames.end())
                {
                    return it->second;
                }
            }

            return std::nullopt;
        }
        catch (...)
        {
            return std::nullopt;
        }

        void ForgetService(DWORD pid)
        {
            m_runningServiceNames.erase(pid);
        }

    private:
        std::map<ULONG, std::wstring> m_runningServiceNames;
    };
}

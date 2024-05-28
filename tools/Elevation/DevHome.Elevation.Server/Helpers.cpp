// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <pch.h>

#include <fstream>
#include <vector>
#include <span>
#include <string>

//#include "PerformanceRecorderEngine.h"
#include "Helpers.h"

/*
void WritePerformanceDataToDisk(_In_ PCWSTR path, const std::span<ProcessPerformanceSummary>& data)
{
    std::ofstream file(path, std::ios::binary);
    if (!file.is_open())
    {
        // Handle error
        return;
    }

    for (const auto& item : data)
    {
        file.write(reinterpret_cast<const char*>(&item), sizeof(ProcessPerformanceSummary));
    }

    file.close();
}

std::vector<ProcessPerformanceSummary> ReadPerformanceDataFromDisk(_In_ PCWSTR path)
{
    std::vector<ProcessPerformanceSummary> data;

    std::ifstream file(path, std::ios::binary);
    THROW_WIN32_IF(ERROR_SHARING_VIOLATION, !file.is_open());

    ProcessPerformanceSummary item;
    while (file.read(reinterpret_cast<char*>(&item), sizeof(ProcessPerformanceSummary)))
    {
        data.push_back(item);
    }

    file.close();
    return data;
}
*/

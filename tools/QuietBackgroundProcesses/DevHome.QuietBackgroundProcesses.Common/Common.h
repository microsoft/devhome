#pragma once

#include <filesystem>

// Get temporary path for performance data
inline std::filesystem::path GetTemporaryPerformanceDataPath()
{
    auto tempDirectory = std::filesystem::temp_directory_path();
    return std::filesystem::path(tempDirectory) / L"DevHome.QuietMode.PerformanceData.dat";
}

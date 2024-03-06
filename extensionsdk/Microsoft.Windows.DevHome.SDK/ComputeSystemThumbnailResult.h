#pragma once
#include "ComputeSystemThumbnailResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct ComputeSystemThumbnailResult : ComputeSystemThumbnailResultT<ComputeSystemThumbnailResult>
    {
        ComputeSystemThumbnailResult(array_view<uint8_t const> thumbnailInBytes);
        ComputeSystemThumbnailResult(winrt::hresult const& e, hstring const& displayMessage, hstring const& diagnosticText);
        com_array<uint8_t> ThumbnailInBytes();
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult Result();

    private:
        array_view<uint8_t const> m_thumbnailInBytes;
        ProviderOperationResult m_result;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct ComputeSystemThumbnailResult : ComputeSystemThumbnailResultT<ComputeSystemThumbnailResult, implementation::ComputeSystemThumbnailResult>
    {
    };
}

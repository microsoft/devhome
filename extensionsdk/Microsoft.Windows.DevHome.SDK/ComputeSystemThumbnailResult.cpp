#include "pch.h"
#include "ComputeSystemThumbnailResult.h"
#include "ComputeSystemThumbnailResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    ComputeSystemThumbnailResult::ComputeSystemThumbnailResult(array_view<uint8_t const> thumbnailInBytes) :
        m_thumbnailInBytes(thumbnailInBytes), m_result(ProviderOperationStatus::Success, S_OK, hstring(), hstring())
    {
    }

    ComputeSystemThumbnailResult::ComputeSystemThumbnailResult(winrt::hresult const& e, hstring const& displayMessage, hstring const& diagnosticText) :
        m_thumbnailInBytes(array_view<uint8_t>{}), m_result(ProviderOperationStatus::Failure, e, displayMessage, diagnosticText)
    {
    }

    com_array<uint8_t> ComputeSystemThumbnailResult::ThumbnailInBytes()
    {
        return com_array<uint8_t>{ m_thumbnailInBytes.begin(), m_thumbnailInBytes.end() };
    }

    ProviderOperationResult ComputeSystemThumbnailResult::Result()
    {
        return m_result;
    }
}

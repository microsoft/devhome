#include "pch.h"
#include "GetFeaturedApplicationsResult.h"
#include "GetFeaturedApplicationsResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    using namespace winrt::Windows::Foundation::Collections;
    using namespace winrt::Microsoft::Windows::DevHome::SDK;

    GetFeaturedApplicationsResult::GetFeaturedApplicationsResult(IVectorView<hstring> const& featuredApplications)
        : m_featuredApplications(featuredApplications), m_result(ProviderOperationStatus::Success, S_OK, hstring(), hstring())
    {
    }

    GetFeaturedApplicationsResult::GetFeaturedApplicationsResult(hresult const& e, hstring const& diagnosticText) :
        m_featuredApplications(nullptr), m_result(ProviderOperationStatus::Failure, e, diagnosticText, diagnosticText)
    {
    }

    IVectorView<hstring> GetFeaturedApplicationsResult::FeaturedApplications()
    {
        return m_featuredApplications;
    }

    ProviderOperationResult GetFeaturedApplicationsResult::Result()
    {
        return m_result;
    }
}

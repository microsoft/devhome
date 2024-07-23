#include "pch.h"
#include "GetFeaturedApplicationsResult.h"
#include "GetFeaturedApplicationsResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    GetFeaturedApplicationsResult::GetFeaturedApplicationsResult(winrt::Windows::Foundation::Collections::IVectorView<hstring> const& featuredApplications)
    {
        throw hresult_not_implemented();
    }
    GetFeaturedApplicationsResult::GetFeaturedApplicationsResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        throw hresult_not_implemented();
    }
    winrt::Windows::Foundation::Collections::IVectorView<hstring> GetFeaturedApplicationsResult::FeaturedApplications()
    {
        throw hresult_not_implemented();
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult GetFeaturedApplicationsResult::Result()
    {
        throw hresult_not_implemented();
    }
}

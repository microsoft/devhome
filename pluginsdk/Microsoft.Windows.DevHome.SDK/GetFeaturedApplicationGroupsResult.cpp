#include "pch.h"
#include "GetFeaturedApplicationGroupsResult.h"
#include "GetFeaturedApplicationGroupsResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    GetFeaturedApplicationGroupsResult::GetFeaturedApplicationGroupsResult(winrt::Windows::Foundation::Collections::IVectorView<winrt::Microsoft::Windows::DevHome::SDK::IFeaturedApplicationGroup> const& featuredApplicationGroups)
    {
        throw hresult_not_implemented();
    }
    GetFeaturedApplicationGroupsResult::GetFeaturedApplicationGroupsResult(winrt::hresult const& e, hstring const& diagnosticText)
    {
        throw hresult_not_implemented();
    }
    winrt::Windows::Foundation::Collections::IVectorView<winrt::Microsoft::Windows::DevHome::SDK::IFeaturedApplicationGroup> GetFeaturedApplicationGroupsResult::FeaturedApplicationGroups()
    {
        throw hresult_not_implemented();
    }
    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult GetFeaturedApplicationGroupsResult::Result()
    {
        throw hresult_not_implemented();
    }
}

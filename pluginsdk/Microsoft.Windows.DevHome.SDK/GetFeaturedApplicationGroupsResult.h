#pragma once
#include "GetFeaturedApplicationGroupsResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct GetFeaturedApplicationGroupsResult : GetFeaturedApplicationGroupsResultT<GetFeaturedApplicationGroupsResult>
    {
        GetFeaturedApplicationGroupsResult() = default;

        GetFeaturedApplicationGroupsResult(winrt::Windows::Foundation::Collections::IVectorView<winrt::Microsoft::Windows::DevHome::SDK::IFeaturedApplicationGroup> const& featuredApplicationGroups);
        GetFeaturedApplicationGroupsResult(winrt::hresult const& e, hstring const& diagnosticText);
        winrt::Windows::Foundation::Collections::IVectorView<winrt::Microsoft::Windows::DevHome::SDK::IFeaturedApplicationGroup> FeaturedApplicationGroups();
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult Result();

    private:
        winrt::Windows::Foundation::Collections::IVectorView<winrt::Microsoft::Windows::DevHome::SDK::IFeaturedApplicationGroup> m_featuredApplicationGroups;
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult m_result;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct GetFeaturedApplicationGroupsResult : GetFeaturedApplicationGroupsResultT<GetFeaturedApplicationGroupsResult, implementation::GetFeaturedApplicationGroupsResult>
    {
    };
}

#pragma once
#include "GetFeaturedApplicationsGroupsResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct GetFeaturedApplicationsGroupsResult : GetFeaturedApplicationsGroupsResultT<GetFeaturedApplicationsGroupsResult>
    {
        GetFeaturedApplicationsGroupsResult() = default;

        GetFeaturedApplicationsGroupsResult(winrt::Windows::Foundation::Collections::IVectorView<winrt::Microsoft::Windows::DevHome::SDK::IFeaturedApplicationsGroup> const& featuredApplicationsGroups);
        GetFeaturedApplicationsGroupsResult(winrt::hresult const& e, hstring const& diagnosticText);
        winrt::Windows::Foundation::Collections::IVectorView<winrt::Microsoft::Windows::DevHome::SDK::IFeaturedApplicationsGroup> FeaturedApplicationsGroups();
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult Result();

    private:
        winrt::Windows::Foundation::Collections::IVectorView<winrt::Microsoft::Windows::DevHome::SDK::IFeaturedApplicationsGroup> m_featuredApplicationsGroups;
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult m_result;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct GetFeaturedApplicationsGroupsResult : GetFeaturedApplicationsGroupsResultT<GetFeaturedApplicationsGroupsResult, implementation::GetFeaturedApplicationsGroupsResult>
    {
    };
}

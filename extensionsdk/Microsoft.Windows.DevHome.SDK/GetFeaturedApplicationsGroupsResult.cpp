#include "pch.h"
#include "GetFeaturedApplicationsGroupsResult.h"
#include "GetFeaturedApplicationsGroupsResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    using namespace winrt::Microsoft::Windows::DevHome::SDK;
    using namespace winrt::Windows::Foundation::Collections;

    GetFeaturedApplicationsGroupsResult::GetFeaturedApplicationsGroupsResult(IVectorView<IFeaturedApplicationsGroup> const& featuredApplicationsGroups)
        : m_featuredApplicationsGroups(featuredApplicationsGroups), m_result(ProviderOperationStatus::Success, S_OK, hstring{}, hstring{})
    {
    }

    GetFeaturedApplicationsGroupsResult::GetFeaturedApplicationsGroupsResult(hresult const& e, hstring const& diagnosticText)
        : m_featuredApplicationsGroups(nullptr), m_result(ProviderOperationStatus::Failure, e, diagnosticText, diagnosticText)
    {
    }

    IVectorView<IFeaturedApplicationsGroup> GetFeaturedApplicationsGroupsResult::FeaturedApplicationsGroups()
    {
        return m_featuredApplicationsGroups;
    }

    ProviderOperationResult GetFeaturedApplicationsGroupsResult::Result()
    {
        return m_result;
    }
}

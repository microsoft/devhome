#include "pch.h"
#include "GetFeaturedApplicationGroupsResult.h"
#include "GetFeaturedApplicationGroupsResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
	using namespace winrt::Microsoft::Windows::DevHome::SDK;
	using namespace winrt::Windows::Foundation::Collections;

    GetFeaturedApplicationGroupsResult::GetFeaturedApplicationGroupsResult(IVectorView<IFeaturedApplicationGroup> const& featuredApplicationGroups)
        : m_featuredApplicationGroups(featuredApplicationGroups), m_result(ProviderOperationStatus::Success, S_OK, hstring{}, hstring{})
    {
    }

    GetFeaturedApplicationGroupsResult::GetFeaturedApplicationGroupsResult(hresult const& e, hstring const& diagnosticText)
        : m_featuredApplicationGroups(nullptr), m_result(ProviderOperationStatus::Failure, e, diagnosticText, diagnosticText)
    {
    }

    IVectorView<IFeaturedApplicationGroup> GetFeaturedApplicationGroupsResult::FeaturedApplicationGroups()
    {
        return m_featuredApplicationGroups;
    }

    ProviderOperationResult GetFeaturedApplicationGroupsResult::Result()
    {
		return m_result;
    }
}

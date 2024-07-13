#pragma once
#include "GetFeaturedApplicationGroupResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct GetFeaturedApplicationGroupResult : GetFeaturedApplicationGroupResultT<GetFeaturedApplicationGroupResult>
    {
        GetFeaturedApplicationGroupResult() = default;

        GetFeaturedApplicationGroupResult(winrt::Microsoft::Windows::DevHome::SDK::IFeaturedApplicationGroup const& featuredApplicationGroup);
        GetFeaturedApplicationGroupResult(winrt::hresult const& e, hstring const& diagnosticText);
        winrt::Microsoft::Windows::DevHome::SDK::IFeaturedApplicationGroup FeaturedApplicationGroup();
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult Result();
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct GetFeaturedApplicationGroupResult : GetFeaturedApplicationGroupResultT<GetFeaturedApplicationGroupResult, implementation::GetFeaturedApplicationGroupResult>
    {
    };
}

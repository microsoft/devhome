// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once
#include "QuickStartProjectResult.g.h"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    struct QuickStartProjectResult : QuickStartProjectResultT<QuickStartProjectResult>
    {
        QuickStartProjectResult(
            array_view<winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectHost const> projectHosts,
            array_view<winrt::Windows::Foundation::Uri const> referenceSamples);

        QuickStartProjectResult(
            array_view<winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectHost const> projectHosts,
            array_view<winrt::Windows::Foundation::Uri const> referenceSamples,
            winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectResultFeedbackHandler const& feedbackHandler);

        QuickStartProjectResult(winrt::hresult const& e, hstring const& displayMessage, hstring const& diagnosticText);

        static winrt::Microsoft::Windows::DevHome::SDK::QuickStartProjectResult CreateWithFeedbackHandler(
            array_view<winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectHost const> projectHosts,
            array_view<winrt::Windows::Foundation::Uri const> referenceSamples,
            winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectResultFeedbackHandler const& feedbackHandler);

        com_array<winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectHost> ProjectHosts();
        winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult Result();
        com_array<winrt::Windows::Foundation::Uri> ReferenceSamples();
        winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectResultFeedbackHandler FeedbackHandler();

        private:
            std::vector<winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectHost> m_projectHosts;
			winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult m_result;
            std::vector<winrt::Windows::Foundation::Uri> m_referenceSamples;
			winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectResultFeedbackHandler m_feedbackHandler;
    };
}
namespace winrt::Microsoft::Windows::DevHome::SDK::factory_implementation
{
    struct QuickStartProjectResult : QuickStartProjectResultT<QuickStartProjectResult, implementation::QuickStartProjectResult>
    {
    };
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "pch.h"
#include "QuickStartProjectResult.h"
#include "QuickStartProjectResult.g.cpp"

namespace winrt::Microsoft::Windows::DevHome::SDK::implementation
{
    QuickStartProjectResult::QuickStartProjectResult(
        array_view<winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectHost const> projectHosts,
        array_view<winrt::Windows::Foundation::Uri const> referenceSamples) :
        m_projectHosts(projectHosts.begin(), projectHosts.end()),
        m_referenceSamples(referenceSamples.begin(), referenceSamples.end()),
        m_result(ProviderOperationStatus::Success, S_OK, hstring(), hstring()),
        m_feedbackHandler(nullptr)
    {
    }

    QuickStartProjectResult::QuickStartProjectResult(
        array_view<winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectHost const> projectHosts,
        array_view<winrt::Windows::Foundation::Uri const> referenceSamples,
        winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectResultFeedbackHandler const& feedbackHandler) :
        m_projectHosts(projectHosts.begin(), projectHosts.end()),
        m_referenceSamples(referenceSamples.begin(), referenceSamples.end()),
        m_result(ProviderOperationStatus::Success, S_OK, hstring(), hstring()),
        m_feedbackHandler(feedbackHandler)
    {
    }

    QuickStartProjectResult::QuickStartProjectResult(winrt::hresult const& e, hstring const& displayMessage, hstring const& diagnosticText) :
        m_projectHosts(std::vector<winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectHost>{}),
        m_referenceSamples(std::vector<winrt::Windows::Foundation::Uri>{}),
		m_result(ProviderOperationStatus::Failure, e, displayMessage, diagnosticText),
        m_feedbackHandler(nullptr)
    {
    }

    winrt::Microsoft::Windows::DevHome::SDK::QuickStartProjectResult QuickStartProjectResult::CreateWithFeedbackHandler(
        array_view<winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectHost const> projectHosts,
        array_view<winrt::Windows::Foundation::Uri const> referenceSamples,
        winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectResultFeedbackHandler const& feedbackHandler)
    {
        return make<QuickStartProjectResult>(projectHosts, referenceSamples, feedbackHandler);
    }

    com_array<winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectHost> QuickStartProjectResult::ProjectHosts()
    {
        return com_array<winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectHost>(m_projectHosts.begin(), m_projectHosts.end());
    }

    winrt::Microsoft::Windows::DevHome::SDK::ProviderOperationResult QuickStartProjectResult::Result()
    {
        return m_result;
    }

    com_array<winrt::Windows::Foundation::Uri> QuickStartProjectResult::ReferenceSamples()
    {
        return com_array<winrt::Windows::Foundation::Uri>(m_referenceSamples.begin(), m_referenceSamples.end());
    }

    winrt::Microsoft::Windows::DevHome::SDK::IQuickStartProjectResultFeedbackHandler QuickStartProjectResult::FeedbackHandler()
    {
        return m_feedbackHandler;
    }
}

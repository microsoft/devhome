// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include <pch.h>

#include <chrono>
#include <map>
#include <memory>
#include <mutex>

#include <wrl/client.h>
#include <wrl/implements.h>
#include <wrl/module.h>

#include <wil/registry.h>
#include <wil/resource.h>
#include <wil/result_macros.h>
#include <wil/win32_helpers.h>
#include <wil/winrt.h>

#include "Helpers.h"
#include "Utility.h"
#include "DevHome.Elevation.h"

static ABI::DevHome::Elevation::ElevationLevel MandatoryLabelToElevationLevel(LONG mandatoryLabel)
{
    if (mandatoryLabel == SECURITY_MANDATORY_HIGH_RID)
    {
        return ABI::DevHome::Elevation::ElevationLevel::High;
    }
    else if (mandatoryLabel == SECURITY_MANDATORY_MEDIUM_RID)
    {
        return ABI::DevHome::Elevation::ElevationLevel::Medium;
    }
    THROW_HR(E_INVALIDARG);
}

static LONG ElevationLevelToMandatoryLabel(ABI::DevHome::Elevation::ElevationLevel elevationLevel)
{
    if (elevationLevel == ABI::DevHome::Elevation::ElevationLevel::High)
    {
        return SECURITY_MANDATORY_HIGH_RID;
    }
    else if (elevationLevel == ABI::DevHome::Elevation::ElevationLevel::Medium)
    {
        return SECURITY_MANDATORY_MEDIUM_RID;
    }
    THROW_HR(E_INVALIDARG);
}

static ABI::DevHome::Elevation::ElevationLevel GetCallingProcessElevationLevel()
{
    auto level = GetCallingProcessMandatoryLabel();
    return MandatoryLabelToElevationLevel(level);
}

namespace ABI::DevHome::Elevation
{
    class ElevationVoucherManagerStatics WrlFinal :
        public Microsoft::WRL::AgileActivationFactory<
            Microsoft::WRL::Implements<IElevationVoucherManagerStatics>>
    {
        InspectableClassStatic(RuntimeClass_DevHome_Elevation_ElevationVoucherManager, BaseTrust);

    public:
        STDMETHODIMP ActivateInstance(_COM_Outptr_ IInspectable**) noexcept
        {
            return E_NOTIMPL;
        }

        STDMETHODIMP AddVoucherToWillCall(
            /* [in] */ IElevationVoucher* voucher,
            /* [in] */ ABI::Windows::Foundation::TimeSpan /* validDuration*/) noexcept
        try
        {
            // This method must called from an elevated process
            // (Or rather, we'll only allow the voucher to be activated if the caller is as elevated as us.)

            // Get client mandatory label
            auto clientElevationLevel = [&]()
            {
                // Get calling process handle
                auto revert = wil::CoImpersonateClient();
            
                wil::unique_handle clientToken;
                THROW_IF_WIN32_BOOL_FALSE(OpenThreadToken(GetCurrentThread(), TOKEN_QUERY, TRUE, &clientToken));

                return MandatoryLabelToElevationLevel(GetTokenMandatoryLabel(clientToken.get()));
            }();

            // Get our mandatory label
            auto ourElevationLevel = MandatoryLabelToElevationLevel(GetTokenMandatoryLabel(GetCurrentProcessToken()));

            // Get voucher elevation level
            ElevationLevel voucherElevationLevel;
            THROW_IF_FAILED(voucher->get_ElevationLevel(&voucherElevationLevel));

            // Do an access check to make sure the caller is elevated enough to put the voucher in will-call
            THROW_HR_IF(E_ACCESSDENIED, clientElevationLevel < voucherElevationLevel);

            // Do an access check to make sure the voucher's requested access level isn't higher than our server (where zone code will execute)!
            THROW_HR_IF(E_ACCESSDENIED, voucherElevationLevel < ourElevationLevel);

            // Save voucher for a period of time
            HSTRING hstrVoucherName;
            THROW_IF_FAILED(voucher->get_VoucherName(&hstrVoucherName));
            auto voucherName = std::wstring(WindowsGetStringRawBuffer(hstrVoucherName, nullptr));
            {
                std::scoped_lock lock(m_mutex);
                m_vouchers.emplace(voucherName, voucher);
            }

            // Quick and dirty: Delete voucher after 10 seconds without worrying about memory issues for now
            auto th = std::thread([voucherName, this]()
            {
                std::this_thread::sleep_for(std::chrono::seconds(10));
                {
                    std::scoped_lock lock(m_mutex);
                    m_vouchers.erase(voucherName);
                }
            });

            th.detach();

            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP ClaimVoucher(
            /* [in] */ HSTRING voucherName,
            /* [out, retval] */ IElevationVoucher** result) noexcept try
        {
            // Find voucher in m_activatedVouchers
            auto strVoucherName = std::wstring(WindowsGetStringRawBuffer(voucherName, nullptr));

            std::scoped_lock lock(m_mutex);
            auto it = m_vouchers.find(strVoucherName);
            THROW_HR_IF(HRESULT_FROM_WIN32(ERROR_NOT_FOUND), it == m_vouchers.end());

            auto voucherPointer = it->second.get();

            // Get voucher process pid
            uint32_t voucherProcessId;
            THROW_IF_FAILED(voucherPointer->get_ProcessId(&voucherProcessId));

            // Get calling process pid
            DWORD callingProcessPid = GetCallingProcessPid();

            // Ensure client process matches what's stored in the voucher
            if (callingProcessPid != voucherProcessId)
            {
                THROW_WIN32(ERROR_THREAD_NOT_IN_PROCESS);
            }

            // Stop tracking the voucher and return it to unelevated client
            auto voucher = std::move(it->second);
            m_vouchers.erase(it);
            *result = voucher.detach();

            return S_OK;
        }
        CATCH_RETURN()


    private:
        std::mutex m_mutex;
        std::map<std::wstring, wil::com_ptr<ABI::DevHome::Elevation::IElevationVoucher>> m_vouchers;
    };

    ActivatableStaticOnlyFactory(ElevationVoucherManagerStatics);
}

namespace ABI::DevHome::Elevation
{
    class ElevationVoucher :
        public Microsoft::WRL::RuntimeClass<
            Microsoft::WRL::RuntimeClassFlags<Microsoft::WRL::WinRt>,
            IElevationVoucher,
            Microsoft::WRL::FtmBase>
    {
        InspectableClass(RuntimeClass_DevHome_Elevation_ElevationVoucher, BaseTrust);

    public:
        STDMETHODIMP RuntimeClassInitialize(HSTRING voucherName, ElevationLevel requestedElevationLevel, ElevationZone zoneId, uint32_t processId, ABI::Windows::Foundation::DateTime processCreateTime) noexcept try
        {
            m_voucherName = std::wstring(WindowsGetStringRawBuffer(voucherName, nullptr));
            m_zoneId = zoneId;
            m_processId = processId;
            m_processCreateTime = processCreateTime;

            if (requestedElevationLevel > GetCallingProcessElevationLevel())
            {
                THROW_WIN32(ERROR_PRIVILEGE_NOT_HELD);
            }

            m_elevationLevel = requestedElevationLevel;
            return S_OK;
        }
        CATCH_RETURN()

        STDMETHODIMP get_VoucherName(_Out_ HSTRING* result) noexcept
        {
            Microsoft::WRL::Wrappers::HStringReference(m_voucherName.c_str()).CopyTo(result);
            return S_OK;
        }

        STDMETHODIMP get_ElevationLevel(_Out_ ElevationLevel* result) noexcept
        {
            *result = m_elevationLevel;
            return S_OK;
        }

        STDMETHODIMP get_ZoneId(_Out_ ElevationZone* result) noexcept
        {
            *result = m_zoneId;
            return S_OK;
        }

        STDMETHODIMP get_ProcessId(_Out_ unsigned int* result) noexcept
        {
            *result = m_processId;
            return S_OK;
        }

        STDMETHODIMP get_ProcessCreateTime(_Out_ Windows::Foundation::DateTime* result) noexcept
        {
            *result = m_processCreateTime;
            return S_OK;
        }

        STDMETHODIMP Redeem(_COM_Outptr_ IElevationZone** result) noexcept
        {
            THROW_IF_FAILED(MakeElevationZone(m_zoneId, result));
            return S_OK;
        }

    private:
        std::wstring m_voucherName;
        ElevationLevel m_elevationLevel;
        ElevationZone m_zoneId;
        uint32_t m_processId;
        ABI::Windows::Foundation::DateTime m_processCreateTime;
    };

    class ElevationVoucherFactory WrlFinal :
        public Microsoft::WRL::AgileActivationFactory<
            Microsoft::WRL::Implements<IElevationVoucherFactory>>
    {
        InspectableClassStatic(RuntimeClass_DevHome_Elevation_ElevationVoucher, BaseTrust);

    public:
        
        STDMETHODIMP ActivateInstance(_COM_Outptr_ IInspectable**) noexcept
        {
            return E_NOTIMPL;
        }

        STDMETHODIMP CreateInstance(
            /* [in] */ HSTRING voucherName,
            ElevationLevel requestedElevationLevel,
            /* [in] */ ElevationZone zoneId,
            /* [in] */ UINT32 processId,
            /* [in] */ ABI::Windows::Foundation::DateTime processCreateTime,
            /* [out, retval] */ IElevationVoucher** result) noexcept
        {
            auto voucher = Microsoft::WRL::Make<ElevationVoucher>();
            THROW_IF_FAILED(voucher->RuntimeClassInitialize(voucherName, requestedElevationLevel, zoneId, processId, processCreateTime));
            *result = voucher.Detach();
            return S_OK;
        }
    };

    ActivatableClassWithFactory(ElevationVoucher, ElevationVoucherFactory);
}

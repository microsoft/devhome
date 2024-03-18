
#define STATUS_INFO_LENGTH_MISMATCH ((NTSTATUS)0xC0000004L) 

typedef enum _THREADINFOCLASS22
{
    ThreadBasicInformation = 0,
    ThreadTimes = 1,
    ThreadPriority = 2,
    ThreadBasePriority = 3,
    ThreadAffinityMask = 4,
    ThreadImpersonationToken = 5,
    ThreadDescriptorTableEntry = 6,
    ThreadEnableAlignmentFaultFixup = 7,
    ThreadEventPair_Reusable = 8,
    ThreadQuerySetWin32StartAddress = 9,
    ThreadZeroTlsCell = 10,
    ThreadPerformanceCount = 11,
    ThreadAmILastThread = 12,
    ThreadIdealProcessor = 13,
    ThreadPriorityBoost = 14,
    ThreadSetTlsArrayAddress = 15, // Obsolete
    ///// ThreadIsIoPending = 16,
    ThreadHideFromDebugger = 17,
    ThreadBreakOnTermination = 18,
    ThreadSwitchLegacyState = 19,
    ThreadIsTerminated = 20,
    ThreadLastSystemCall = 21,
    ThreadIoPriority = 22,
    ThreadCycleTime = 23,
    ThreadPagePriority = 24,
    ThreadActualBasePriority = 25,
    ThreadTebInformation = 26,
    ThreadCSwitchMon = 27, // Obsolete
    ThreadCSwitchPmu = 28,
    ThreadWow64Context = 29,
    ThreadGroupInformation = 30,
    ThreadUmsInformation = 31, // UMS
    ThreadCounterProfiling = 32,
    ThreadIdealProcessorEx = 33,
    ThreadCpuAccountingInformation = 34,
    ThreadSuspendCount = 35, // Threshold
    // Threshold_endx_ntddk
    ThreadHeterogeneousCpuPolicy = 36, // Threshold
    ThreadContainerId = 37, // Threshold
    //// ThreadNameInformation = 38,
    // Threshold_beginx_ntddk
    ThreadReserved1Information = 36, // Threshold
    ThreadReserved2Information = 37, // Threshold
    MaxThreadInfoClass = 39,
} THREADINFOCLASS22;

THREADINFOCLASS tic(THREADINFOCLASS22 input)
{
    return (THREADINFOCLASS)input;
}

typedef struct _THREAD_CYCLE_TIME_INFORMATION
{
    ULONG64 AccumulatedCycles;
    ULONG64 CurrentCycleCount;
} THREAD_CYCLE_TIME_INFORMATION, *PTHREAD_CYCLE_TIME_INFORMATION;






namespace internal_types
{
    typedef struct _SYSTEM_PROCESS_INFORMATION
    {
        //using public_type = ::SYSTEM_PROCESS_INFORMATION;

        ULONG NextEntryOffset;
        ULONG NumberOfThreads;
        LARGE_INTEGER WorkingSetPrivateSize;
        ULONG HardFaultCount;
        ULONG NumberOfThreadsHighWatermark;
        ULONGLONG CycleTime;
        LARGE_INTEGER CreateTime;
        LARGE_INTEGER UserTime;
        LARGE_INTEGER KernelTime;
        UNICODE_STRING ImageName;
        KPRIORITY BasePriority;
        HANDLE UniqueProcessId;
        HANDLE InheritedFromUniqueProcessId;
        ULONG HandleCount;
        ULONG SessionId;
        ULONG_PTR UniqueProcessKey;
        SIZE_T PeakVirtualSize;
        SIZE_T VirtualSize;
        ULONG PageFaultCount;
        SIZE_T PeakWorkingSetSize;
        SIZE_T WorkingSetSize;
        SIZE_T QuotaPeakPagedPoolUsage;
        SIZE_T QuotaPagedPoolUsage;
        SIZE_T QuotaPeakNonPagedPoolUsage;
        SIZE_T QuotaNonPagedPoolUsage;
        SIZE_T PagefileUsage;
        SIZE_T PeakPagefileUsage;
        SIZE_T PrivatePageCount;
        LARGE_INTEGER ReadOperationCount;
        LARGE_INTEGER WriteOperationCount;
        LARGE_INTEGER OtherOperationCount;
        LARGE_INTEGER ReadTransferCount;
        LARGE_INTEGER WriteTransferCount;
        LARGE_INTEGER OtherTransferCount;
    } SYSTEM_PROCESS_INFORMATION, *PSYSTEM_PROCESS_INFORMATION;
}

/*
*/
template <typename T>
struct public_type_traits;

#define MAKE_PUBLIC_TYPE_TRAITS(type) \
    template <>                        \
    struct public_type_traits<type>    \
    {                                  \
        using internal_type = internal_types::type; \
    };

/*
template <>
struct public_type_traits<SYSTEM_PROCESS_INFORMATION>
{
    using internal_type = internal_types::SYSTEM_PROCESS_INFORMATION;
};
*/
MAKE_PUBLIC_TYPE_TRAITS(SYSTEM_PROCESS_INFORMATION);


template <typename FromT, typename ToT>
const ToT& anycast(const FromT& input)
{
    return *reinterpret_cast<const ToT*>(&input);
}

/*
template<typename PublicT>
sdf<PublicT> to_internal_type(const PublicT& input);

template<>
internal_types::SYSTEM_PROCESS_INFORMATION to_internal_type<SYSTEM_PROCESS_INFORMATION>(const SYSTEM_PROCESS_INFORMATION& input)
{
    return static_cast<internal_types::SYSTEM_PROCESS_INFORMATION>(input);
}
*/
template<typename PublicT>
const typename public_type_traits<PublicT>::internal_type& as_internal_type(const PublicT& input)
{
    //return reinterpret_cast<public_type_traits<PublicT>::internal_type>(input);
    //return *reinterpret_cast<public_type_traits<PublicT>::internal_type*>(&input);
    return anycast<PublicT, public_type_traits<PublicT>::internal_type>(input);
    //return (typename public_type_traits<PublicT>::internal_type)input;
}


__kernel_entry NTSTATUS
    NTAPI
    NtQuerySystemInformation(
        IN SYSTEM_INFORMATION_CLASS ,
        OUT PVOID ,
        IN ULONG ,
        OUT PULONG  )
{
    return STATUS_INFO_LENGTH_MISMATCH;
}

__kernel_entry NTSTATUS
    NTAPI
    NtQueryInformationThread(
        IN HANDLE ,
        IN THREADINFOCLASS ,
        OUT PVOID ,
        IN ULONG ,
        OUT PULONG ReturnLength )
{
    return STATUS_INFO_LENGTH_MISMATCH;
}



/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.01.0628 */
/* at Mon Jan 18 19:14:07 2038
 */
/* Compiler settings for C:\Users\SERGEY~1.RED\AppData\Local\Temp\DevHome.Service.idl-82e54409:
    Oicf, W1, Zp8, env=Win64 (32b run), target_arch=AMD64 8.01.0628 
    protocol : all , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
/* @@MIDL_FILE_HEADING(  ) */



/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 500
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif /* __RPCNDR_H_VERSION__ */

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __DevHome2EService_h_p_h__
#define __DevHome2EService_h_p_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

#ifndef DECLSPEC_XFGVIRT
#if defined(_CONTROL_FLOW_GUARD_XFG)
#define DECLSPEC_XFGVIRT(base, func) __declspec(xfg_virtual(base, func))
#else
#define DECLSPEC_XFGVIRT(base, func)
#endif
#endif

/* Forward Declarations */ 

#ifndef ____x_DevHome_CService_CITimServer_FWD_DEFINED__
#define ____x_DevHome_CService_CITimServer_FWD_DEFINED__
typedef interface __x_DevHome_CService_CITimServer __x_DevHome_CService_CITimServer;

#endif 	/* ____x_DevHome_CService_CITimServer_FWD_DEFINED__ */


/* header files for imported files */
#include "inspectable.h"
#include "windows.foundation.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_DevHome2EService_0000_0000 */
/* [local] */ 




extern RPC_IF_HANDLE __MIDL_itf_DevHome2EService_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_DevHome2EService_0000_0000_v0_0_s_ifspec;

#ifndef ____x_DevHome_CService_CITimServer_INTERFACE_DEFINED__
#define ____x_DevHome_CService_CITimServer_INTERFACE_DEFINED__

/* interface __x_DevHome_CService_CITimServer */
/* [object][uuid] */ 


EXTERN_C const IID IID___x_DevHome_CService_CITimServer;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("f0fdac8b-19da-5298-ad85-aea3c7be2281")
    __x_DevHome_CService_CITimServer : public IInspectable
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetNumber2( 
            /* [out] */ long *number) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct __x_DevHome_CService_CITimServerVtbl
    {
        BEGIN_INTERFACE
        
        DECLSPEC_XFGVIRT(IUnknown, QueryInterface)
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            __x_DevHome_CService_CITimServer * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        DECLSPEC_XFGVIRT(IUnknown, AddRef)
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            __x_DevHome_CService_CITimServer * This);
        
        DECLSPEC_XFGVIRT(IUnknown, Release)
        ULONG ( STDMETHODCALLTYPE *Release )( 
            __x_DevHome_CService_CITimServer * This);
        
        DECLSPEC_XFGVIRT(IInspectable, GetIids)
        HRESULT ( STDMETHODCALLTYPE *GetIids )( 
            __x_DevHome_CService_CITimServer * This,
            /* [out] */ ULONG *iidCount,
            /* [size_is][size_is][out] */ IID **iids);
        
        DECLSPEC_XFGVIRT(IInspectable, GetRuntimeClassName)
        HRESULT ( STDMETHODCALLTYPE *GetRuntimeClassName )( 
            __x_DevHome_CService_CITimServer * This,
            /* [out] */ HSTRING *className);
        
        DECLSPEC_XFGVIRT(IInspectable, GetTrustLevel)
        HRESULT ( STDMETHODCALLTYPE *GetTrustLevel )( 
            __x_DevHome_CService_CITimServer * This,
            /* [out] */ TrustLevel *trustLevel);
        
        DECLSPEC_XFGVIRT(__x_DevHome_CService_CITimServer, GetNumber2)
        HRESULT ( STDMETHODCALLTYPE *GetNumber2 )( 
            __x_DevHome_CService_CITimServer * This,
            /* [out] */ long *number);
        
        END_INTERFACE
    } __x_DevHome_CService_CITimServerVtbl;

    interface __x_DevHome_CService_CITimServer
    {
        CONST_VTBL struct __x_DevHome_CService_CITimServerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define __x_DevHome_CService_CITimServer_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define __x_DevHome_CService_CITimServer_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define __x_DevHome_CService_CITimServer_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define __x_DevHome_CService_CITimServer_GetIids(This,iidCount,iids)	\
    ( (This)->lpVtbl -> GetIids(This,iidCount,iids) ) 

#define __x_DevHome_CService_CITimServer_GetRuntimeClassName(This,className)	\
    ( (This)->lpVtbl -> GetRuntimeClassName(This,className) ) 

#define __x_DevHome_CService_CITimServer_GetTrustLevel(This,trustLevel)	\
    ( (This)->lpVtbl -> GetTrustLevel(This,trustLevel) ) 


#define __x_DevHome_CService_CITimServer_GetNumber2(This,number)	\
    ( (This)->lpVtbl -> GetNumber2(This,number) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* ____x_DevHome_CService_CITimServer_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif



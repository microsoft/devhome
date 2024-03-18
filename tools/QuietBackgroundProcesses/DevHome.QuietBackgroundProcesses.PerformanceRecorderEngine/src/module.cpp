#include <windows.h>

#include <wil\resource.h>
#include <wil\result.h>


HINSTANCE g_hInstance = nullptr;

extern "C" BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID)
{
    switch (dwReason)
    {
    case DLL_PROCESS_ATTACH:
        ::DisableThreadLibraryCalls(hInstance);
        g_hInstance = hInstance;
        break;

    case DLL_PROCESS_DETACH:
        break;
    }

    return TRUE;
}

extern "C" HRESULT WINAPI DllCanUnloadNow()
{
    return S_OK;
}

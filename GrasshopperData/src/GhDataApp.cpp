#include "StdAfx.h"
#include <afxdllx.h>

AC_IMPLEMENT_EXTENSION_MODULE(SampleDLL)

extern "C" BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
    UNREFERENCED_PARAMETER(lpReserved) ;

    if (DLL_PROCESS_ATTACH == dwReason)
    {
        _hdllInstance = hInstance;
        SampleDLL.AttachInstance(hInstance);
        InitAcUiDLL();
    }
    else if (DLL_PROCESS_DETACH == dwReason)
    {
        SampleDLL.DetachInstance();
    }
    return TRUE;
}

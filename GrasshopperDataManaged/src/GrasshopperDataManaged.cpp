#include "StdAfx.h"

//-----------------------------------------------------------------------------
//- DLL Entry Point
#pragma unmanaged

WCHAR _DllPath[MAX_PATH];

extern "C"
BOOL WINAPI DllMain (HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
    UNREFERENCED_PARAMETER(lpReserved) ;

    if ( dwReason == DLL_PROCESS_ATTACH )
    {
        GetModuleFileNameW(hInstance, _DllPath, MAX_PATH);
        //_hdllInstance =hInstance ; // Disabled to remove loader-lock contention during managed loads...
    }
    else if( dwReason == DLL_PROCESS_DETACH )
    {
    }

    return (TRUE) ;
}
#pragma managed

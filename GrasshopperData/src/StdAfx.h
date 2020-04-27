#pragma once

#define STRICT

#ifndef WINVER
#define WINVER 0x0501
#endif

#ifndef VC_EXTRALEAN
#define VC_EXTRALEAN
#endif

//-----------------------------------------------------------------------------
//- 'DEBUG workaround' below prevents the MFC or ATL #include-s from pulling
//- in "Afx.h" that would force the debug CRT through #pragma-s.
#if defined(_WIN32) && defined(_DEBUG) && !defined(BRX_BCAD_DEBUG)
  #define _DEBUG_WAS_DEFINED
  #undef _DEBUG
  #define NDEBUG
  #pragma message ("     Compiling MFC / STL / ATL header files in release mode.")
#endif

//-----------------------------------------------------------------------------------
/*
MFC includes
*/
#include <afxwin.h>
#include <afxext.h>
#include <afxcmn.h>

//-----------------------------------------------------------------------------------
/*
ARX or BRX includes
*/
#include "arxHeaders.h"

//-----------------------------------------------------------------------------
#ifdef _DEBUG_WAS_DEFINED
  #undef NDEBUG
  #define _DEBUG
  #undef _DEBUG_WAS_DEFINED
#endif
//-----------------------------------------------------------------------------

#include "brx_version.h"

// Copyright (C) Menhirs NV. All rights reserved.
// License to copy and modify this file for usage in conjunction
// with Autodesk ARX API is explicitely expressed here, as long as the
// copyright statement remains unchanged !
// Please copy this file to your Autodesk ARX API or project folder(s).

// this header profides following features :
// it defines a number of useful macros, to manage multi-platform/version/architecture builds

#pragma once

#ifndef SYS_VERSION_INCLUDED
#define SYS_VERSION_INCLUDED

  // ========== for Windows, Linux, Mac =========

  // these predefined symbols should help developers to recognise build version and
  // architecture by simply using BRX SDK (this file is automatically included);
  // eliminates the need to define own symbols for all build configurations

  #undef __SYS_WINDOWS            // if yet defined elsewhere
  #undef __SYS_LINUX              // if yet defined elsewhere
  #undef __SYS_LINUX_64_BIT__     // if yet defined elsewhere
  #undef __SYS_MAC                // if yet defined elsewhere
  #undef __SYS_MAC_64_BIT__       // if yet defined elsewhere
  #undef __SYS_64_BIT__           // if yet defined elsewhere
  #undef __SYS_32_BIT__           // if yet defined elsewhere
  #undef __SYS_LINUXMAC_64_BIT__  // if yet defined elsewhere
  #undef _LIN64                   // for backward compatibility
  #undef _MAC64                   // for backward compatibility
  #undef _LINUXMAC64              // for backward compatibility

  #ifdef _MSC_VER
    #define __SYS_WINDOWS
  #elif defined(__linux__)
    #define __SYS_LINUX
  #elif defined(__APPLE__) || defined(_MAC)
    #define __SYS_MAC
  #endif

  #if defined(__LP64__) || defined(_LP64) || defined(__64BITS__) || defined(__x86_64__) || defined ( _WIN64 )
    #define __SYS_64_BIT__
  #else
    #define __SYS_32_BIT__
  #endif

  #if defined(__SYS_64_BIT__) && defined(__SYS_LINUX)
    #define __SYS_LINUX_64_BIT__
    #define _LIN64
  #endif

  #if defined(__SYS_64_BIT__) && defined(__SYS_MAC)
    #define __SYS_MAC_64_BIT__
    #define _MAC64
  #endif

  #if defined(__SYS_LINUX_64_BIT__) || defined(__SYS_MAC_64_BIT__)
    #define __SYS_LINUXMAC_64_BIT__
    #define _LINUXMAC64
  #endif


#endif // SYS_VERSION_INCLUDED

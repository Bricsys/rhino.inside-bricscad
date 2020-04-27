#ifdef GHDATA_API
#define GH_IMPORTEXPORT  __declspec(dllexport)
#else
#define GH_IMPORTEXPORT  __declspec(dllimport)
#endif

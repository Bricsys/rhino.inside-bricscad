#include "StdAfx.h"
#include "mgdinterop.h"
#include "GrasshopperData.h"

static AcMgObjectFactoryBase** pp_ObjFct = NULL;

extern WCHAR _DllPath[MAX_PATH];
WCHAR GhDataApp_Path[MAX_PATH];

class GrassopperDataManaged : public AcRxDbxApp {

public:
    GrassopperDataManaged () : AcRxDbxApp () {}

    virtual AcRx::AppRetCode On_kInitAppMsg (void *pAppData) 
    {
        AcRx::AppRetCode retMsg = AcRxDbxApp::On_kInitAppMsg(pAppData);

        //init path to GhDataApp.dll
        {
            wcscpy_s(GhDataApp_Path, _DllPath);
            PathRemoveFileSpecW(GhDataApp_Path);
            PathAppendW(GhDataApp_Path, L"GhDataApp.dll");
            //acutPrintf(GhDataApp_Path);
        }

        acedArxLoad(GhDataApp_Path);
        RegisterManagedWrapperLink();
        //acutPrintf(_T("\nRegistered GhDataManaged.dll"));
        return retMsg;
    }

    virtual AcRx::AppRetCode On_kUnloadAppMsg (void *pAppData) 
    {
        UnregisterManagedWrapperLink();
        acedArxUnload(GhDataApp_Path);
        AcRx::AppRetCode retMsg = AcRxDbxApp::On_kUnloadAppMsg(pAppData);
        return retMsg;
    }

private:
    void RegisterManagedWrapperLink()
    {
        static AcMgObjectFactoryBase* p_ObjFctArr[] = 
        {
            new AcMgObjectFactory<GH_BC::GrasshopperData,DbGrasshopperData>(),
            NULL
        };
        pp_ObjFct = p_ObjFctArr;
    }
    void UnregisterManagedWrapperLink()
    {
        if (!pp_ObjFct)
            return;
        int i=0;
        while (pp_ObjFct[i]!=NULL)
        {
            AcMgObjectFactoryBase*& pFactory = pp_ObjFct[i++];
            delete pFactory;
            pFactory = NULL;
        }
        pp_ObjFct = NULL;
    }

    virtual void RegisterServerComponents () 
    {
    }
} ;

IMPLEMENT_ARX_ENTRYPOINT(GrassopperDataManaged)

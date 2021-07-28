#include "StdAfx.h"
#include "GrasshopperOPMExtension.h"
#ifdef _DEBUG
#include "DbGrasshopperData.h"
#include "GhProperty.h"
#endif

class GhDataApp: public AcRxArxApp
{
public:
    GhDataApp() : AcRxArxApp() {}

    virtual void RegisterServerComponents()
    {
    }

    virtual AcRx::AppRetCode On_kInitAppMsg(void* pAppData)
    {
        AcRx::AppRetCode result = AcRxArxApp::On_kInitAppMsg(pAppData);
        acrxRegisterAppMDIAware(pAppData); // is able to work in MDI context
        registerGhOPMExtension();
#ifdef _DEBUG
        acutPrintf(_T("\nRegistered GhDataApp.dll"));
#endif
        return result;
    }

    virtual AcRx::AppRetCode On_kUnloadAppMsg(void* pAppData)
    {
        unregisterGhOPMExtension();
        return AcRxArxApp::On_kUnloadAppMsg(pAppData);
    }

    virtual AcRx::AppRetCode On_kLoadDwgMsg(void* pAppData)
    {
        return AcRxArxApp::On_kLoadDwgMsg(pAppData);
    }

    virtual AcRx::AppRetCode On_kUnloadDwgMsg(void* pAppData)
    {
        return AcRxArxApp::On_kUnloadDwgMsg(pAppData);
    }

    virtual AcRx::AppRetCode On_kQuitMsg(void* pAppData)
    {
        return AcRxArxApp::On_kQuitMsg(pAppData);
    }

#ifdef _DEBUG
    static void GhSampleAttachGh(void)
    {
        ads_name en;
        ads_point pt;
        if (RTNORM != acedEntSel(_T("\nSelect an entity: "), en, pt))
        {
            acutPrintf(_T("\nError during object selection"));
            return;
        }
        AcDbObjectId objId;
        acdbGetObjectId(objId, en);
        AcDbObjectPointer<AcDbEntity> pDbObj(objId, AcDb::kForWrite);
        if (pDbObj.openStatus() != eOk)
            return;

        auto pData = new DbGrasshopperData(L"E:\\box.gh");
        pData->addProperty(L"A", GhProperty(true));
        pData->addProperty(L"B", GhProperty(50.5));
        pData->addProperty(L"C", GhProperty(50));
        pData->addProperty(L"D", GhProperty(AcString(L"desc")));
        pData->addProperty(L"E", GhProperty(AcGePoint3d(10, 15, 20)));
        pData->addProperty(L"F", GhProperty(AcGeVector3d(10, 15, 20)));
        DbGrasshopperData::attachGrasshopperData(pDbObj, pData);
        pData->close();
    }

    static void GhSampleRemoveGh(void)
    {
        ads_name en;
        ads_point pt;
        if (RTNORM != acedEntSel(_T("\nSelect an entity: "), en, pt))
        {
            acutPrintf(_T("\nError during object selection"));
            return;
        }
        AcDbObjectId objId;
        acdbGetObjectId(objId, en);
        AcDbObjectPointer<AcDbEntity> pDbObj(objId, AcDb::kForWrite);
        if (pDbObj.openStatus() != eOk)
            return;

        DbGrasshopperData::removeGrasshopperData(pDbObj);
    }
#endif
};

IMPLEMENT_ARX_ENTRYPOINT(GhDataApp)

#ifdef _DEBUG
ACED_ARXCOMMAND_ENTRY_AUTO(GhDataApp, GhSample, AttachGh, AttachGh, ACRX_CMD_TRANSPARENT, NULL)
ACED_ARXCOMMAND_ENTRY_AUTO(GhDataApp, GhSample, RemoveGh, RemoveGh, ACRX_CMD_TRANSPARENT, NULL)
#endif

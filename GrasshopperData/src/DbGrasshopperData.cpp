#include "StdAfx.h"
#include "DbGrasshopperData.h"
#include "GhProperty.h"

static const ACHAR* s_ghData = L"GrasshopperData";

ACRX_DXF_DEFINE_MEMBERS(DbGrasshopperData,AcDbObject,AcDb::kDHL_CURRENT,AcDb::kMReleaseCurrent,
                        AcDbProxyEntity::kNoOperation,DbGrasshopperData,"Grasshopper-BricsCAD-Connection")

#define CLASS_VERSION 0

DbGrasshopperData::DbGrasshopperData()
{}

DbGrasshopperData::DbGrasshopperData(const ACHAR* definition) : m_definition(definition)
{}

DbGrasshopperData::~DbGrasshopperData()
{}

AcString DbGrasshopperData::getDefinition() const
{
    assertReadEnabled();
    return m_definition;
}

void DbGrasshopperData::setDefinition(const ACHAR* definition)
{
    assertWriteEnabled();
    m_definition = definition;
}

bool DbGrasshopperData::getVisibility() const
{
    assertReadEnabled();
    return m_isVisible;
}

void DbGrasshopperData::setVisibility(bool v)
{
    assertWriteEnabled();
    m_isVisible = v;
}

GhPropertyTypeArray DbGrasshopperData::getPropertiesTypes() const
{
    assertReadEnabled();
    GhPropertyTypeArray res;
    res.reserve(m_props.size());
    for (const auto& prop : m_props)
        res.emplace_back(prop.first, prop.second->getType());
    return res;
}

GhProperty DbGrasshopperData::getProperty(const AcString& name) const
{
    assertReadEnabled();
    auto i = m_props.find(name);
    return i == m_props.end() ? GhProperty() : *i->second;
}

bool DbGrasshopperData::updateProperty(const AcString& name, const GhProperty& value)
{
    if (value.isEmpty())
        return false;

    auto i = m_props.find(name);
    if (i == m_props.end())
        return false;

    if (i->second->getType() != value.getType())
        return false;

    assertWriteEnabled();
    (*i->second) = value;
    return true;
}

bool DbGrasshopperData::addProperty(const AcString& name, const GhProperty& value)
{
    auto i = m_props.lower_bound(name);
    if ((i != m_props.end() && i->first == name) || value.isEmpty())
        return false;

    assertWriteEnabled();
    m_props.emplace_hint(i, name, std::make_unique<GhProperty>(value));
    return true;
}

void DbGrasshopperData::clearProperties()
{
    assertWriteEnabled();
    m_props.clear();
}

AcDbObjectId DbGrasshopperData::getHostEntity() const
{
    assertReadEnabled();
    auto dictId = ownerId();
    if (dictId.isNull())
        return {};

    AcDbObjectPointer<AcDbDictionary> pDict(dictId, AcDb::kForRead);
    if (!pDict.object())
        return {};

    return pDict->ownerId();
}

Acad::ErrorStatus DbGrasshopperData::dwgOutFields(AcDbDwgFiler* pFiler) const
{
    assertReadEnabled();

    Acad::ErrorStatus status = AcDbObject::dwgOutFields(pFiler);
    if (Acad::eOk != status)
        return status;

    pFiler->writeUInt8(CLASS_VERSION);
    pFiler->writeString(m_definition);
    pFiler->writeItem(m_isVisible);
    pFiler->writeItem(m_props.size());
    for (const auto& prop : m_props)
    {
        pFiler->writeString(prop.first);
        prop.second->dwgOutFields(pFiler);
    }
    return pFiler->filerStatus();
}

Acad::ErrorStatus DbGrasshopperData::dwgInFields(AcDbDwgFiler* pFiler)
{
    assertWriteEnabled();
    m_props.clear();
    Acad::ErrorStatus status = AcDbObject::dwgInFields(pFiler);
    if (Acad::eOk != status)
        return status;

    Adesk::UInt8 version;
    pFiler->readUInt8(&version);
    if (version < 0 || version > CLASS_VERSION)
        return Acad::eMakeMeProxy;

    pFiler->readString(m_definition);
    pFiler->readItem(&m_isVisible);
    size_t propSize;
    pFiler->readItem(&propSize);
    for (size_t i = 0; i < propSize; ++i)
    {
        AcString propName;
        pFiler->readString(propName);
        GhProperty prop;
        prop.dwgInFields(pFiler);
        addProperty(propName, prop);
    }
    return pFiler->filerStatus();
}

AcDbObjectId DbGrasshopperData::getGrasshopperData(const AcDbEntity* pEnt)
{
    if (!pEnt)
        return {};

    auto dictId = pEnt->extensionDictionary();
    if (dictId.isNull())
        return {};

    AcDbObjectPointer<AcDbDictionary> pDict(dictId, AcDb::kForRead);
    if (pDict.openStatus() != eOk)
        return {};

    AcDbObjectId ghDataId;
    if (eOk == pDict->getAt(s_ghData, ghDataId))
        return ghDataId;

    return {};
}

bool DbGrasshopperData::attachGrasshopperData(AcDbEntity* pEnt, DbGrasshopperData* pData)
{
    if (!pEnt || !pData || pData->objectId())
        return false;

    auto dictId = pEnt->extensionDictionary();
    if (dictId.isNull())
    {
        pEnt->upgradeOpen();
        pEnt->createExtensionDictionary();
        dictId = pEnt->extensionDictionary();
    }

    if (dictId.isNull())
        return false;

    AcDbObjectPointer<AcDbDictionary> pDict(dictId, AcDb::kForRead);
    if (pDict.openStatus() != eOk)
        return {};

    AcDbObjectId ghDataId;
    if (eOk != pDict->getAt(s_ghData, ghDataId))
    {
        pDict->upgradeOpen();
        pDict->setAt(s_ghData, pData, ghDataId);
        return true;
    }
    return false;
}

void DbGrasshopperData::removeGrasshopperData(AcDbEntity* pEnt)
{
    auto ghId = getGrasshopperData(pEnt);
    if (ghId.isNull())
        return;

    auto dictId = pEnt->extensionDictionary();
    AcDbObjectPointer<AcDbDictionary> pDict(dictId, AcDb::kForWrite);
    if (pDict.openStatus() != eOk)
        return;

    pDict->remove(ghId);
    AcDbObjectPointer<DbGrasshopperData> pGhData(ghId, AcDb::kForWrite);
    if (pGhData.openStatus() == eOk)
        pGhData->erase();
}

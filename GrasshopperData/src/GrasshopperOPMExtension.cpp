#include "StdAfx.h"
#include <tchar.h>

#include "AcDb/AcDbEntity.h"
#include "AcDb/AcDbObjectPointer.h"

#include "GrasshopperOPMExtension.h"
#include "DbGrasshopperData.h"
#include "GhProperty.h"

static GrasshopperDataOPM* s_GhOPMExtension = nullptr;

const AcOPMPropertyId AcOPMPROP_DefinitionProp = AcOPMPROP_FirstUserProp;
const AcOPMPropertyId AcOPMPROP_VisibilityProp = AcOPMPROP_FirstUserProp + 1;
const AcOPMPropertyId AcOPMPROP_LastProp = AcOPMPROP_VisibilityProp;

static const AcString s_GhCategoryName = _T("Grasshopper Data");

static AcOpmDataType toOpm(GhProperty::Type type)
{
    switch (type)
    {
    case GhProperty::eInt:
        return opmTypeInteger;
    case GhProperty::eReal:
        return opmTypeDouble;
    case GhProperty::eBool:
        return opmTypeBool;
    case GhProperty::eString:
        return opmTypeString;
    case GhProperty::ePoint:
        return opmTypePoint3d;
    case GhProperty::eVector:
        return opmTypeVector3d;
    }
    assert(false);
    return opmTypeNone;
}

template<typename T>
bool initOpmVariant(const GhProperty& prop, AcOPMVariant& opmValue)
{
    T val;
    if (prop.getValue(val))
    {
        opmValue = val;
        return true;
    }
    return false;
}

static bool toOpm(const GhProperty& prop, AcOPMVariant& value)
{
    switch (prop.getType())
    {
    case GhProperty::eInt:
        return initOpmVariant<int>(prop, value);
    case GhProperty::eReal:
        return initOpmVariant<double>(prop, value);
    case GhProperty::eBool:
        return initOpmVariant<bool>(prop, value);
    case GhProperty::eString:
        return initOpmVariant<AcString>(prop, value);
    case GhProperty::ePoint:
        return initOpmVariant<AcGePoint3d>(prop, value);
    case GhProperty::eVector:
        return initOpmVariant<AcGeVector3d>(prop, value);
    }
    return false;
}

template<typename T>
bool initGhProperty(const AcOPMVariant& value, GhProperty& ghProp)
{
    T val;
    return value.get(val) && ghProp.setValue(val);
}

static GhProperty toGh(const AcOPMVariant& value)
{
    switch (value.type())
    {
    case AcOPMVariant::kInteger:
    {
        GhProperty prop(GhProperty::eInt);
        initGhProperty<int>(value, prop);
        return prop;
    }
    case AcOPMVariant::kDouble:
    {
        GhProperty prop(GhProperty::eReal);
        initGhProperty<double>(value, prop);
        return prop;
    }
    case AcOPMVariant::kBool:
    {
        GhProperty prop(GhProperty::eBool);
        initGhProperty<bool>(value, prop);
        return prop;
    }
    case AcOPMVariant::kString:
    {
        GhProperty prop(GhProperty::eString);
        initGhProperty<AcString>(value, prop);
        return prop;
    }
    case AcOPMVariant::kPoint:
    {
        GhProperty prop(GhProperty::ePoint);
        initGhProperty<AcGePoint3d>(value, prop);
        return prop;
    }
    case AcOPMVariant::kVector:
    {
        GhProperty prop(GhProperty::eVector);
        initGhProperty<AcGeVector3d>(value, prop);
        return prop;
    }
    }
    return {};
}

bool registerGhOPMExtension()
{
    s_GhOPMExtension = new GrasshopperDataOPM();
    AcOpmResult res = acRegisterEntityExtension(s_GhOPMExtension, AcDbEntity::desc(), true);
    return (res == opmNoError);
}

bool unregisterGhOPMExtension()
{
    bool res = acRemoveOPMExtension(s_GhOPMExtension);
    delete s_GhOPMExtension, s_GhOPMExtension = nullptr;
    return true;
}

AcOPMPropertyId GrasshopperDataOPM::getIdFromName(const AcString& name) const
{    
    static AcOPMPropertyId curId = AcOPMPROP_LastProp;
    auto i = m_nameToId.lower_bound(name);
    if (i != m_nameToId.end() && i->first == name)
        return i->second;

    m_nameToId.emplace_hint(i, name, ++curId);
    m_idToName[curId] = name;
    return curId;
}

AcString GrasshopperDataOPM::getNameFromId(AcOPMPropertyId id) const
{
    auto i = m_idToName.find(id);
    return i == m_idToName.end() ? AcString() : i->second;
}

bool GrasshopperDataOPM::getApplicationName(AcString& name) const
{
    name = _T("Grasshopper-BricsCAD connection");
    return true;
}

bool GrasshopperDataOPM::supportsDynamicProperties() const
{
    return true;
}

bool GrasshopperDataOPM::getDynamicPropertyMap(const AcDbEntity* entity,
                                               AcOPMPropertyArray& properties) const
{
    auto id = DbGrasshopperData::getGrasshopperData(entity);
    if (id.isNull())
        return true;

    AcDbObjectPointer<DbGrasshopperData> pGhData(id);
    if (pGhData.openStatus() != eOk)
        return false;

    AcOPMPropertyEntry propDefinition(s_GhCategoryName, _T("Definition"),
        AcOPMPROP_DefinitionProp, AcOpmDataType(opmTypeString | opmFlagNoUserEdit), true);
    properties.append(propDefinition);

    AcOPMPropertyEntry propVisibility(s_GhCategoryName, _T("Gh-visibility"),
        AcOPMPROP_VisibilityProp, opmTypeCheckBox, true);
    properties.append(propVisibility);

    for (const auto& prop : pGhData->getPropertiesTypes())
    {
        AcOPMPropertyEntry dynProp(s_GhCategoryName, prop.first,
            getIdFromName(prop.first), toOpm(prop.second), true);
        properties.append(dynProp);
    }
    return true;
}

bool GrasshopperDataOPM::getPropertyValue(const AcDbEntity* entity,
                                          AcOPMPropertyId propertyId,
                                          const AcString& childName,
                                          AcOPMVariant& value) const
{
    auto id = DbGrasshopperData::getGrasshopperData(entity);
    if (id.isNull())
        return false;

    AcDbObjectPointer<DbGrasshopperData> pGhData(id);
    if (pGhData.openStatus() != eOk)
        return false;

    switch (propertyId)
    {
    case AcOPMPROP_DefinitionProp:
        value = pGhData->getDefinition();
        return true;
    case AcOPMPROP_VisibilityProp:
        value = pGhData->getVisibility();
        return true;
    }

    auto propName = getNameFromId(propertyId);
    if (propName.isEmpty())
        return false;

    auto propValue = pGhData->getProperty(propName);
    assert(!propValue.isEmpty());
    return toOpm(propValue, value);
}

bool GrasshopperDataOPM::setPropertyValue(AcDbEntity* entity,
                                          AcOPMPropertyId propertyId,
                                          const AcString& childName,
                                          const AcOPMVariant& value)
{
    auto id = DbGrasshopperData::getGrasshopperData(entity);
    if (id.isNull())
        return false;

    AcDbObjectPointer<DbGrasshopperData> pGhData(id, AcDb::kForWrite);
    if (pGhData.openStatus() != eOk)
        return false;

    AcString sVal;
    bool bVal = false;
    switch (propertyId)
    {
    case AcOPMPROP_DefinitionProp:
        if (value.get(sVal))
        {
            pGhData->setDefinition(sVal);
            return true;
        }
        break;
    case AcOPMPROP_VisibilityProp:
        if (value.get(bVal))
        {
            pGhData->setVisibility(bVal);
            return true;
        }
        break;
    }

    auto propName = getNameFromId(propertyId);
    if (propName.isEmpty())
        return false;

    return pGhData->updateProperty(propName, toGh(value));
}

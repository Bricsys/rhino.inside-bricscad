#pragma once

#include "Export.h"
#include "GhProperty.h"

#include <map>
#include <memory>
#include <vector>

using GhProperties = std::map<AcString, std::unique_ptr<GhProperty>>;
using GhPropertyTypeArray = std::vector<std::pair<AcString, GhProperty::Type>>;

class GH_IMPORTEXPORT DbGrasshopperData : public AcDbObject
{
private:
    // stored in DWG
    AcString m_definition;
    GhProperties m_props;
    bool m_isVisible = false;

public:
    ACRX_DECLARE_MEMBERS(DbGrasshopperData);

public:
    DbGrasshopperData();
    DbGrasshopperData(DbGrasshopperData&) = delete;
    DbGrasshopperData& operator=(DbGrasshopperData&) = delete;
    DbGrasshopperData(const ACHAR*);
    virtual ~DbGrasshopperData();

    AcString getDefinition() const;
    void setDefinition(const ACHAR*);

    bool getVisibility() const;
    void setVisibility(bool v);

    GhPropertyTypeArray getPropertiesTypes() const;
    GhProperty getProperty(const AcString& name) const;
    bool updateProperty(const AcString& name, const GhProperty& value);
    bool addProperty(const AcString& name, const GhProperty& value);
    void clearProperties();

    AcDbObjectId getHostEntity() const;

    static AcDbObjectId getGrasshopperData(const AcDbEntity* pEnt);
    static bool attachGrasshopperData(AcDbEntity* pEnt, DbGrasshopperData* pData);
    static void removeGrasshopperData(AcDbEntity* pEnt);

    //AcDbObject
    Acad::ErrorStatus dwgOutFields(AcDbDwgFiler*) const override;
    Acad::ErrorStatus dwgInFields(AcDbDwgFiler*) override;
};

ACDB_REGISTER_OBJECT_ENTRY_AUTO(DbGrasshopperData)

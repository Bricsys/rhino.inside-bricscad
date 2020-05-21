#pragma once

#include "BrxSpecific/AcOpmExtensions.h"

class GrasshopperDataOPM : public AcOPMClientExtension
{
public:
    GrasshopperDataOPM() = default;
    ~GrasshopperDataOPM() = default;

    bool getApplicationName(AcString& name) const override;

    bool supportsDynamicProperties() const override;

    bool getDynamicPropertyMap(const AcDbEntity* entity,
                               AcOPMPropertyArray& properties) const override;

    bool getPropertyValue(const AcDbEntity* entity,
                          AcOPMPropertyId propertyId,
                          const AcString& childName,
                          AcOPMVariant& value) const override;

    bool setPropertyValue(AcDbEntity* entity,
                          AcOPMPropertyId propertyId,
                          const AcString& childName,
                          const AcOPMVariant& value) override;

private:
    AcOPMPropertyId getIdFromName(const AcString& name) const;
    AcString getNameFromId(AcOPMPropertyId id) const;

    mutable std::map<AcString, AcOPMPropertyId> m_nameToId;
    mutable std::map<AcOPMPropertyId, AcString> m_idToName;
};

bool registerGhOPMExtension();
bool unregisterGhOPMExtension();

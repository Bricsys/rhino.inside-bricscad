#include "StdAfx.h"
#include "GrasshopperData.h"
#include "GhProperty.h"
#include "mgdinterop.h"

using namespace System;

namespace GH_BC
{;

inline System::Object^ ToSystemObject(const GhProperty& prop)
{
    if (prop.isEmpty() || !prop.isSet())
        return nullptr;

    GhProperty::Type type = prop.getType();
    switch (type)
    {
    case GhProperty::Type::eInt:
    {
        int i;
        if (prop.getValue(i))
            return i;
        break;
    }
    case GhProperty::Type::eReal:
    {
        double d;
        if (prop.getValue(d))
            return d;
        break;
    }
    case GhProperty::Type::eBool:
    {
        bool b;
        if (prop.getValue(b))
            return b;
        break;
    }
    case GhProperty::Type::eString:
    {
        AcString str;
        if (prop.getValue(str))
            return gcnew System::String(str.constPtr());
        break;
    }
    case GhProperty::Type::ePoint:
    {
        AcGePoint3d pnt;
        if (prop.getValue(pnt))
            return ToPoint3d(pnt);
        break;
    }
    case GhProperty::Type::eVector:
    {
        AcGeVector3d vec;
        if (prop.getValue(vec))
            return ToVector3d(vec);
        break;
    }
    }
    return nullptr;
}

inline GhProperty ToGhProperty(System::Object^ pObject)
{
    if (nullptr == pObject)
        throw gcnew ArgumentNullException();

    if (pObject->GetType() == int::typeid)
    {
        return static_cast<int>(pObject);
    }
    else if (pObject->GetType() == double::typeid)
    {
        return static_cast<double>(pObject);
    }
    else if (pObject->GetType() == bool::typeid)
    {
        return static_cast<bool>(pObject);
    }
    else if (pObject->GetType() == String::typeid)
    {
        pin_ptr< const wchar_t > pStr = PtrToStringChars((String^)pObject);
        return AcString(pStr);
    }
    else if (pObject->GetType() == Teigha::Geometry::Point3d::typeid)
    {
        return GETPOINT3D(pObject);
    }
    else if (pObject->GetType() == Teigha::Geometry::Vector3d::typeid)
    {
        return GETVECTOR3D(pObject);
    }
    return {};
}

inline GhProperty ToGhProperty(System::Type^ type)
{
    if (nullptr == type)
        throw gcnew ArgumentNullException("Should not be null");

    if (type == int::typeid)
        return GhProperty(GhProperty::Type::eInt);
    else if (type == double::typeid)
        return GhProperty(GhProperty::Type::eReal);
    else if (type == bool::typeid)
        return GhProperty(GhProperty::Type::eBool);
    else if (type == String::typeid)
        return GhProperty(GhProperty::Type::eString);
    else if (type == Teigha::Geometry::Point3d::typeid)
        return GhProperty(GhProperty::Type::ePoint);
    else if (type == Teigha::Geometry::Vector3d::typeid)
        return GhProperty(GhProperty::Type::eVector);
    return {};
}

GrasshopperData::GrasshopperData() : DBObject(mgdCtorHelper(new DbGrasshopperData), true)
{}

GrasshopperData::GrasshopperData(System::String^ definition) : 
                                 DBObject(mgdCtorHelper(new DbGrasshopperData), true)
{
    Definition = definition;
}

GrasshopperData::GrasshopperData(System::IntPtr unmanagedPointer, bool autoDelete) :
                                       DBObject(mgdCtorHelper(unmanagedPointer), autoDelete)
{}

DbGrasshopperData* GrasshopperData::GetImpObj()
{
     return getImpObjHelper<DbGrasshopperData>(UnmanagedObject);
}

String^ GrasshopperData::Definition::get()
{
    AcString ghDef = this->GetImpObj()->getDefinition();
    return ghDef.isEmpty() ? nullptr : gcnew String(ghDef.constPtr());
}

void GrasshopperData::Definition::set(System::String^ value)
{
    pin_ptr<const wchar_t> pStr = PtrToStringChars(value);
    this->GetImpObj()->setDefinition(pStr);
}

System::Boolean GrasshopperData::IsVisible::get()
{
    return this->GetImpObj()->getVisibility();
}

void GrasshopperData::IsVisible::set(System::Boolean value)
{
    this->GetImpObj()->setVisibility(value);
}

Teigha::DatabaseServices::ObjectId GrasshopperData::HostEntity::get()
{
    return ToObjectId(this->GetImpObj()->getHostEntity());
}

Teigha::DatabaseServices::ObjectId GrasshopperData::GetGrasshopperData(Teigha::DatabaseServices::Entity^ entity)
{
    auto pAcEnt = getImpObjHelper<AcDbEntity>(entity->UnmanagedObject);
    auto acObjId = DbGrasshopperData::getGrasshopperData(pAcEnt);
    return ToObjectId(acObjId);
}

System::Object^ GrasshopperData::GetProperty(System::String^ propertyName)
{
    auto pGhData = this->GetImpObj();
    pin_ptr<const wchar_t> pStr = PtrToStringChars(propertyName);
    auto ghProp = pGhData->getProperty(pStr);
    return ToSystemObject(ghProp);
}

System::Boolean GrasshopperData::UpdateProperty(System::String^ propertyName, System::Object^ value)
{
    pin_ptr<const wchar_t> pStr = PtrToStringChars(propertyName);
    auto ghProp = ToGhProperty(value);
    return this->GetImpObj()->updateProperty(pStr, ghProp);
}

System::Boolean GrasshopperData::AddProperty(System::String^ propertyName, System::Object^ value)
{
    pin_ptr<const wchar_t> pStr = PtrToStringChars(propertyName);
    auto ghProp = ToGhProperty(value);
    return this->GetImpObj()->addProperty(pStr, ghProp);
}

System::Boolean GrasshopperData::AddProperty(System::String^ propertyName, System::Type^ type)
{
    pin_ptr<const wchar_t> pStr = PtrToStringChars(propertyName);
    auto ghProp = ToGhProperty(type);
    return this->GetImpObj()->addProperty(pStr, ghProp);
}

void GrasshopperData::ClearProperties()
{
    this->GetImpObj()->clearProperties();
}

void GrasshopperData::RemoveGrasshopperData(Teigha::DatabaseServices::Entity^ entity)
{
    auto pAcEnt = getImpObjHelper<AcDbEntity>(entity->UnmanagedObject);
    DbGrasshopperData::removeGrasshopperData(pAcEnt);
}

System::Boolean GrasshopperData::AttachGrasshopperData(Teigha::DatabaseServices::Entity^ entity, GrasshopperData^ ghData)
{
    auto pAcEnt = getImpObjHelper<AcDbEntity>(entity->UnmanagedObject);
    return DbGrasshopperData::attachGrasshopperData(pAcEnt, ghData->GetImpObj());
}

};

#pragma once

#include "Export.h"
#include <memory>

class AcGeVector3d;
class AcGePoint3d;

class GH_IMPORTEXPORT GhProperty
{
public:
    enum Type : Adesk::UInt8
    {
        eEmpty,
        eInt,
        eReal,
        eBool,
        eString,
        ePoint,
        eVector
    };

    GhProperty();
    GhProperty(Type type);
    GhProperty(const GhProperty&);
    GhProperty& operator =(const GhProperty& other);
    ~GhProperty();

    GhProperty(int);
    GhProperty(double);
    GhProperty(bool);
    GhProperty(const AcString&);
    GhProperty(const AcGeVector3d&);
    GhProperty(const AcGePoint3d&);
    
    bool getValue(int&) const;
    bool getValue(double&) const;
    bool getValue(bool&) const;
    bool getValue(AcString&) const;
    bool getValue(AcGeVector3d&) const;
    bool getValue(AcGePoint3d&) const;

    bool setValue(int);
    bool setValue(double);
    bool setValue(bool);
    bool setValue(const AcString&);
    bool setValue(const AcGeVector3d&);
    bool setValue(const AcGePoint3d&);

    Type getType() const;
    bool isSet() const;
    bool isEmpty() const;

    Acad::ErrorStatus dwgOutFields(AcDbDwgFiler*) const;
    Acad::ErrorStatus dwgInFields(AcDbDwgFiler*);

private:
    class Impl;
    std::unique_ptr<Impl> m_pImpl = nullptr;
};

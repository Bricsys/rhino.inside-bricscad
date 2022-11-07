#pragma once

#include "Stdafx.h"
#include "mgdinterop.h"
#include "DbGrasshopperData.h"

class DbGrasshopperData;

namespace GH_BC
{
    [Teigha::Runtime::Wrapper("DbGrasshopperData")]
    public ref class GrasshopperData : public Teigha::DatabaseServices::DBObject
    {
    public:
        GrasshopperData();
        GrasshopperData(System::String^ definition);

    public protected:
        GrasshopperData(System::IntPtr unmanagedPointer, bool autoDelete);
        DbGrasshopperData*  GetImpObj();

    public:
        property System::String^ Definition
        {
            System::String^ get();
            void set(System::String^);
        }

        property System::Boolean IsVisible
        {
            System::Boolean get();
            void set(System::Boolean);
        }

        property Teigha::DatabaseServices::ObjectId HostEntity
        {
            Teigha::DatabaseServices::ObjectId get();
        }

        System::Object^ GetProperty(System::String^ propertyName);
        System::Boolean UpdateProperty(System::String^ propertyName, System::Object^ value);
        System::Boolean AddProperty(System::String^ propertyName, System::Object^ value);
        System::Boolean AddProperty(System::String^ propertyName, System::Type^ type);
        void ClearProperties();

        static Teigha::DatabaseServices::ObjectId GetGrasshopperData(Teigha::DatabaseServices::Entity^);
        static void RemoveGrasshopperData(Teigha::DatabaseServices::Entity^);
        static System::Boolean AttachGrasshopperData(Teigha::DatabaseServices::Entity^, GrasshopperData^);
    };
}

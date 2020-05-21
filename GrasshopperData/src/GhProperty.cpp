#include "StdAfx.h"
#include "GhProperty.h"

#define SUPPORTED_TYPES()\
ON_PRIMITIVE(int)        \
ON_PRIMITIVE(bool)       \
ON_PRIMITIVE(double)     \
ON_COMPLEX(AcString)     \
ON_COMPLEX(AcGePoint3d)  \
ON_COMPLEX(AcGeVector3d)

#define TYPE_MAP()              \
MAP_ENTRY(int, eInt)            \
MAP_ENTRY(bool, eBool)          \
MAP_ENTRY(double, eReal)        \
MAP_ENTRY(AcString, eString)    \
MAP_ENTRY(AcGePoint3d, ePoint)  \
MAP_ENTRY(AcGeVector3d, eVector)

template <class T>
struct TypeInfo;

#define MAP_ENTRY(DataType, TypeId)\
template <>\
struct TypeInfo<DataType>\
{\
    static const GhProperty::Type Value = GhProperty::Type::TypeId;\
};
TYPE_MAP()
#undef MAP_ENTRY

template<typename AType>
struct ByValue
{
    using ArgumentType = AType;
};

template<typename AType>
struct ByRef
{
    using ArgumentType = AType const&;
};

template<typename AType, typename Enable = void>
struct DataTraits : ByRef<AType> {};

template<typename AType>
struct DataTraits<AType, typename std::enable_if<std::is_fundamental<AType>::value, void>::type> : ByValue<AType> {};

class PropertyDataBase
{
public:
    virtual ~PropertyDataBase() = default;
    static PropertyDataBase* create(GhProperty::Type);

    virtual PropertyDataBase* clone() const = 0;
    virtual void dwgOutFields(AcDbDwgFiler* pFiler) const = 0;
    virtual void dwgInFields(AcDbDwgFiler* pFiler) = 0;
};

template <typename T>
class PropertyData : public PropertyDataBase
{
public:
    PropertyData() = default;
    PropertyData(typename DataTraits<T>::ArgumentType data) : m_data(data) {}
    ~PropertyData() = default;

    typename DataTraits<T>::ArgumentType getData() const
    {
        return m_data;
    }

    void setData(typename DataTraits<T>::ArgumentType data)
    {
        m_data = data;
    }

    PropertyDataBase* clone() const
    {
        return new PropertyData(m_data);
    }

    void dwgOutFields(AcDbDwgFiler* pFiler) const override
    {
        pFiler->writeItem(m_data);
    }

    void dwgInFields(AcDbDwgFiler* pFiler) override
    {
        pFiler->readItem(&m_data);
    }

private:
    T m_data;
};

void PropertyData<AcString>::dwgInFields(AcDbDwgFiler* pFiler)
{
    pFiler->readString(m_data);
}

void PropertyData<int>::dwgInFields(AcDbDwgFiler* pFiler)
{
    pFiler->readInt32((Adesk::Int32*)&m_data);
}

void PropertyData<int>::dwgOutFields(AcDbDwgFiler* pFiler) const
{
    pFiler->writeInt32(m_data);
}

PropertyDataBase* PropertyDataBase::create(GhProperty::Type type)
{
    switch (type)
    {
#define MAP_ENTRY(DataType, TypeId)\
    case GhProperty::TypeId: return new PropertyData<DataType>();
        TYPE_MAP()
#undef MAP_ENTRY
    }
    return nullptr;
}

class GhProperty::Impl
{
public:
    Impl(GhProperty::Type type) : m_type(type), m_data(PropertyDataBase::create(type))
    {}
    
    ~Impl()
    {
        if (m_data)
            delete m_data;
    }

    Impl* clone()
    {        
        return new Impl(m_type, m_data ? m_data->clone() : nullptr, m_isSet);
    }

    template <typename T>
    bool setValue(typename DataTraits<T>::ArgumentType val)
    {
        if (!m_data || m_type != TypeInfo<T>::Value)
            return false;

        m_isSet = true;
        static_cast<PropertyData<T>*>(m_data)->setData(val);
        return true;
    }
    
    template <typename T>
    bool getValue(T& val) const
    {
        if (!m_isSet && !m_data || m_type != TypeInfo<T>::Value)
            return false;
        val = static_cast<PropertyData<T>*>(m_data)->getData();
        return true;
    }

    bool isSet() const
    {
        return m_isSet;
    }

    Acad::ErrorStatus dwgOutFields(AcDbDwgFiler* pFiler) const
    {
        pFiler->writeUInt8(m_type);
        pFiler->writeItem(m_isSet);
        if (m_isSet)
            m_data->dwgOutFields(pFiler);
        return Acad::ErrorStatus::eOk;
    }

    Acad::ErrorStatus dwgInFields(AcDbDwgFiler* pFiler)
    {
        if (m_data)
            delete m_data;

        m_type = [&]() {
            Adesk::UInt8 type;
            pFiler->readUInt8(&type);
            return static_cast<GhProperty::Type>(type);
        }();
        pFiler->readItem(&m_isSet);
        m_data = PropertyDataBase::create(m_type);
        if (m_isSet)
            m_data->dwgInFields(pFiler);
        return Acad::ErrorStatus::eOk;
    }

private:
    Impl(GhProperty::Type type, PropertyDataBase* data, bool isSet) : m_type(type), m_data(data), m_isSet(isSet)
    {}

    PropertyDataBase* m_data = nullptr;
    GhProperty::Type m_type = eEmpty;
    bool m_isSet = false;
    friend GhProperty;
};

#define ON_PRIMITIVE(DataType)\
GhProperty::GhProperty(DataType v) : m_pImpl(new Impl(TypeInfo<DataType>::Value))\
{\
    m_pImpl->setValue<DataType>(v);\
}\
bool GhProperty::getValue(DataType& v) const { return m_pImpl->getValue(v); }\
bool GhProperty::setValue(DataType v) { return m_pImpl->setValue<DataType>(v); }
#define ON_COMPLEX(DataType)\
GhProperty::GhProperty(const DataType& v) : m_pImpl(new Impl(TypeInfo<DataType>::Value))\
{\
    m_pImpl->setValue<DataType>(v);\
}\
bool GhProperty::getValue(DataType& v) const { return m_pImpl->getValue(v); }\
bool GhProperty::setValue(const DataType& v) { return m_pImpl->setValue<DataType>(v); }
SUPPORTED_TYPES()
#undef ON_PRIMITIVE
#undef ON_COMPLEX

GhProperty::Type GhProperty::getType() const
{
    return m_pImpl->m_type;
}

bool GhProperty::isSet() const
{
    return !isEmpty() && m_pImpl->isSet();
}

bool GhProperty::isEmpty() const
{
    return m_pImpl->m_type == eEmpty;
}

GhProperty::GhProperty() : m_pImpl(std::make_unique<Impl>(eEmpty))
{}

GhProperty::GhProperty(Type type) : m_pImpl(std::make_unique<Impl>(type))
{}

GhProperty::GhProperty(const GhProperty& other)
{
    m_pImpl.reset(other.m_pImpl->clone());
}

GhProperty& GhProperty::operator=(const GhProperty& other)
{
    m_pImpl.reset(other.m_pImpl->clone());
    return *this;
}

GhProperty::~GhProperty()
{}

Acad::ErrorStatus GhProperty::dwgOutFields(AcDbDwgFiler* pFiler) const
{
    return m_pImpl->dwgOutFields(pFiler);
}

Acad::ErrorStatus GhProperty::dwgInFields(AcDbDwgFiler* pFiler)
{
    return m_pImpl->dwgInFields(pFiler);
}

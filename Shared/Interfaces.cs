//UInterfaces.pas

using System.Xml;

namespace Shared;

internal interface ISerializable {
    void Serialize(XmlElement xmlElement);
}

internal interface IInvalidate {
    void Invalidate();
}
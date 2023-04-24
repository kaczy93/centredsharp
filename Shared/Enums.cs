using System.Xml.Serialization;

namespace CentrED; 

public enum LoginState {
    Ok,
    InvalidUser,
    InvalidPassword,
    AlreadyLoggedIn,
    NoAccess
}

public enum ServerState {
    Running,
    Frozen,
    Other
}

public enum AccessLevel {
    [XmlEnum("0")]
    None,
    [XmlEnum("1")]
    View,
    [XmlEnum("2")]
    Normal,
    [XmlEnum("3")]
    Developer,
    [XmlEnum("255")]
    Administrator = 255
}

public enum ModifyUserStatus {
    InvalidUsername,
    Added,
    Modified
}

public enum DeleteUserStatus {
    NotFound,
    Deleted
}

public enum ModifyRegionStatus {
    Added,
    Modified
}

public enum DeleteRegionStatus {
    NotFound,
    Deleted
}

public enum PasswordChangeStatus {
    Success,
    OldPwInvalid,
    NewPwInvalid,
    Identical
}

public enum ProtocolVersion {
    CentrED = 6,
    CentrEDPlus = 0x1008
}
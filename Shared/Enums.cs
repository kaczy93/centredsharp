namespace Shared; 

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
    None,
    View,
    Normal,
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
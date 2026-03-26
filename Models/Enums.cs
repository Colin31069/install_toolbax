namespace InstallToolbox.Models;

public enum SourceType
{
    Winget,
    DirectUrl,
    LocalFile
}

public enum InstallerType
{
    Winget,
    Exe,
    Msi,
    Zip
}

public enum AppStatus
{
    Pending,
    Checking,
    Skipped,
    Downloading,
    Installing,
    Verifying,
    Success,
    Retrying,
    Failed,
    Cancelled
}

public enum InstallCheckType
{
    Registry,
    Path,
    Winget
}

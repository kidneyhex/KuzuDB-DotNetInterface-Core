# .NET Standard 2.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET Standard 2.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET Standard 2.0 upgrade.
3. Upgrade KuzuDB-Net\KuzuDB-Net.csproj


## Settings

This section contains settings and data used by execution steps.

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### KuzuDB-Net\KuzuDB-Net.csproj modifications

Project properties changes:
  - Convert project from legacy .NET Framework format to SDK-style project format
  - Target framework should be changed from `net48` to `netstandard2.0`

Other changes:
  - Project file needs to be converted to SDK-style format to support .NET Standard 2.0
@echo off
cls

IF NOT EXIST packages\FAKE\tools\FAKE.exe  (
  .nuget\nuget.exe install FAKE -OutputDirectory packages -ExcludeVersion
  .nuget\nuget.exe install SourceLink.Fake -OutputDirectory packages -ExcludeVersion
)

IF NOT EXIST build.fsx (
  packages\FAKE\tools\FAKE.exe init.fsx
)

IF NOT EXIST packages\xunit.runner.console\tools\xunit.console.exe (
  .nuget\nuget.exe install xunit.runner.console -OutputDirectory packages -ExcludeVersion
)

packages\FAKE\tools\FAKE.exe build.fsx %*
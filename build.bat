@echo off
cls

IF NOT EXIST packages\FAKE\tools\FAKE.exe  (
  .nuget\nuget.exe install FAKE -OutputDirectory packages -ExcludeVersion
  .nuget\nuget.exe install SourceLink.Fake -OutputDirectory packages -ExcludeVersion
)

IF NOT EXIST build.fsx (
  packages\FAKE\tools\FAKE.exe init.fsx
)

IF NOT EXIST packages\xunit.runners\tools\xunit.console.clr4.exe (
  .nuget\nuget.exe install xunit.runners -OutputDirectory packages -ExcludeVersion
)

packages\FAKE\tools\FAKE.exe build.fsx %*
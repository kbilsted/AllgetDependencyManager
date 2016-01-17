namespace AllgetDependencyManager

type PackageVersion = { Version: string; SortableName: string }
type Package = { Name: string; Version: PackageVersion}
type ConfigurationRow  = { ProjectName: string; NugetPackageName: string; Version: PackageVersion }

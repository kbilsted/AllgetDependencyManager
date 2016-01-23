namespace AllgetDependencyManager

[<AutoOpen>]
module infrastructure =
    type PackageName = PackageName of string 
    type ProjectName = ProjectName of string 

    type PackageVersion = { Version: string; SortableName: string }
    type Package = { Name: PackageName; Version: PackageVersion}
    type ConfigurationRow  = { ProjectName: ProjectName; PackageName: PackageName; Version: PackageVersion }
    type NugetInfoPackage  = { Name: PackageName; Version: string}

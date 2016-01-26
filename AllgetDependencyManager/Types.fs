namespace AllgetDependencyManager

[<AutoOpen>]
module infrastructure =

    let rec Reduce filterfunc filters entries =
        match filters with
        | [] -> entries
        | filter::xs -> 
            let newentries = filterfunc filter entries 
            Reduce filterfunc xs newentries 
        
    type PackageName = PackageName of string 
    type ProjectName = ProjectName of string 

    type PackageVersion = { Version: string; SortableName: string }
    type Package = { Name: PackageName; Version: PackageVersion}
    type ConfigurationRow  = { ProjectName: ProjectName; PackageName: PackageName; Version: PackageVersion }
    type NugetInfoPackage  = { Name: PackageName; Version: string}

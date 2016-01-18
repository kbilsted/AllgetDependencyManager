namespace AllgetDependencyManager

open System.IO

module CompositionRoot =
    open AllgetDependencyManager.Nuget.NugetConfigurationParser
    open AllgetDependencyManager.Nuget.NugetApiGateway
    open System.IO

    let SelectAll = 
        Directory.GetFileSystemEntries(".", "packages.config", SearchOption.AllDirectories)
        |> Seq.map(fun f -> ParseNugetPackageConfig f (File.ReadAllText(f)))
        |> Seq.collect(fun f -> f)
        |> Seq.where(fun f -> f.ProjectName <> ".nuget" )
    
    let SelectNugetPackage =
        SelectAll 
        |> Seq.map(fun f -> {Package.Name=f.NugetPackageName; Package.Version=f.Version}) 
        |> Seq.distinctBy(fun f -> f.Version.Version)
        |> Seq.sortBy(fun f -> f.Version.SortableName)
        |> Seq.groupBy(fun f -> f.Name)

    let SelectLatestVersions configrows = 
        configrows 
        |> Seq.map(fun f -> (f, GetLatestVersion f))
        |> Seq.where(fun (f,version) -> version.IsSome)
        |> Seq.map(fun (f,version) -> f, {NugetInfoPackage.Name = f; Version = version.Value})        
        |> dict

    [<EntryPoint>]
    let main argv = 
        printfn "%A" argv
        SelectAll
        |> Seq.iter(fun f -> printfn "%s %s " f.ProjectName f.NugetPackageName)

        printfn " -- each dependency and all its versions -- "
        for (p,vs) in SelectNugetPackage do
            printfn "%s" p
            for v in vs do
                printfn "*  %s (%s)" v.Version.Version v.Version.SortableName


        ignore SelectAll

        0 // return an integer exit code

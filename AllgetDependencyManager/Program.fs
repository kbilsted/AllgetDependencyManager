namespace AllgetDependencyManager

open System.IO

module CompositionRoot =
    open AllgetDependencyManager.NugetConfigurationParser
    open AllgetDependencyManager.NugetApiGateway

    let SelectAll = 
        Directory.GetFileSystemEntries(".", "packages.config", SearchOption.AllDirectories)
        |> Seq.map(fun f -> ParseNugetPackageConfig f (File.ReadAllText(f)))
        |> Seq.collect(fun f -> f)
        |> Seq.where(fun f -> let (ProjectName name) = f.ProjectName in name <> ".nuget")
    
    let SelectNugetPackage =
        SelectAll 
        |> Seq.map(fun f -> {Package.Name=f.PackageName; Package.Version=f.Version}) 
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

        // --- 
        printfn " -- all projects -- "
        SelectAll
        |> Seq.distinctBy(fun f-> f.ProjectName)
        |> Seq.iter(fun f -> printfn "%A" f.ProjectName)

        // ---
        printfn " -- each dependency and all its versions -- "
        let dependencies = SelectNugetPackage
        for (p,vs) in SelectNugetPackage do
            printfn "%A" p
            for v in vs do
                printfn "  * %s (%s)" v.Version.Version v.Version.SortableName


        // ---
        printfn " -- each dependency and all its versions and latest nuget -- "
        let dependencies = SelectNugetPackage
//        let cheat = SelectAll |> Seq.map(fun f -> f.ProjectName) |> SelectLatestVersions // WRONG TYPE - works due to type aliasing
        let latestVersions = SelectLatestVersions (dependencies |> Seq.map(fun (name, versions) -> name))
        for (p,vs) in SelectNugetPackage do
            printfn "%A" p
            let latestVersion = latestVersions.[p].Version
            let hasLatestVersion = vs |> Seq.exists(fun f -> f.Version.Version = latestVersion)
            for v in vs do
                if latestVersion = v.Version.Version then
                    printfn "  * %s (%s) (Nuget)" v.Version.Version v.Version.SortableName
                else
                    printfn "  * %s (%s)" v.Version.Version v.Version.SortableName
            if not hasLatestVersion then
                printfn "* Nuget version: %s" latestVersion

        ignore SelectAll

        0 // return an integer exit code

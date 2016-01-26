namespace AllgetDependencyManager

open System.IO

module CommandLineParsing =
    open System.Text.RegularExpressions
    
    type MatchKind = Include | Exclude
    type FilterInstruction = FilterInstruction of MatchKind * Regex

    type CommandLineConfiguration = { 
            ProjectsFilter: FilterInstruction list; 
        }
    
    let ParseCommandLine args : CommandLineConfiguration =
        let AddConfig config kind filter =
            let instruction = FilterInstruction(Include, new Regex(filter, RegexOptions.Compiled))
            { config with ProjectsFilter = instruction::config.ProjectsFilter }

        let rec parseIth args config =
            match args with 
            | [] -> config
            | "-iproj"::xs -> 
                match xs with
                | filter::xss -> parseIth xss (AddConfig config Include filter)
                | [] -> failwith "Missing filter argument for option '-iproj'"
            | "-eproj"::xs ->
                match xs with
                | filter::xss -> parseIth xss (AddConfig config Exclude filter)
                | [] -> failwith "Missing filter argument for option '-eproj'"
            | x::xs -> failwith <| sprintf "I don't understand %s" x
        
        parseIth args { ProjectsFilter = [FilterInstruction(Exclude, new Regex("^\.nuget$"))] }


module CompositionRoot =
    open AllgetDependencyManager.NugetConfigurationParser
    open AllgetDependencyManager.NugetApiGateway
    open CommandLineParsing

    let SelectAll = 
        Directory.GetFileSystemEntries(".", "packages.config", SearchOption.AllDirectories)
        |> Seq.map(fun f -> ParseNugetPackageConfig f (File.ReadAllText(f)))
        |> Seq.collect(fun f -> f)
        |> Seq.where(fun f -> let (ProjectName name) = f.ProjectName in name <> ".nuget")
    
    let SelectNugetPackage configrows =
        configrows
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
        let commandlineconf = ParseCommandLine (Array.toList argv)

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

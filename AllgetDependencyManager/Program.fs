namespace AllgetDependencyManager

open System.IO

type PackageVersion = {Version: string; SortableName: string }
type Package = {Name: string; Version: PackageVersion}
type ConfigurationRow  = { ProjectName: string; NugetPackageName: string; Version: PackageVersion }


module NugetConfigurationParser =
    open FSharp.Data
    open System.IO
    open System

    type PackageConfigFormat = XmlProvider<"""<?xml version="1.0" encoding="utf-8"?>
        <packages>
          <package id="Some.Data" version="a2.2.5" targetFramework="net46" />
          <package id="Anot.Data" version="2.2.5-alpha" targetFramework="net46" />
        </packages>""">    

    let createNugetVersion (version: string) =
        let rec zeropad size (input: string) =
            if input.Length < size then zeropad size ("0"+input) else input

        let sortableName = 
            version.Split([|'.'|])  
            |> Seq.map(fun f -> zeropad 5 f)  
            |> String.concat "."   
        {
            Version = version; 
            SortableName = sortableName;
        }

    let GetFolderOfFile (path: string) =
        (new DirectoryInfo(Path.GetDirectoryName(path))).Name

    let ParseNugetPackageConfig path content =
        PackageConfigFormat.Parse(content).Packages 
        |> Seq.map(fun f -> { ProjectName = GetFolderOfFile path; NugetPackageName = f.Id; Version = createNugetVersion f.Version })


module CompositionRoot =
    open NugetConfigurationParser
    open System.IO

    let SelectAll = 
        Directory.GetFileSystemEntries(".", "packages.config", SearchOption.AllDirectories)
        |> Seq.map(fun f -> ParseNugetPackageConfig f (File.ReadAllText(f)))
        |> Seq.collect(fun f -> f)
    
    let SelectNugetPackage =
        SelectAll 
        |> Seq.map(fun f -> {Name=f.NugetPackageName; Version=f.Version}) 
        |> Seq.distinctBy(fun f -> f.Version.Version)
        |> Seq.sortBy(fun f -> f.Version.SortableName)
        |> Seq.groupBy(fun f -> f.Name)

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

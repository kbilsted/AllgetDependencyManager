namespace AllgetDependencyManager.Nuget

open AllgetDependencyManager

module NugetConfigurationParser =
    open FSharp.Data
    open System.IO

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


module NugetApiGateway =
    open FSharp.Data

    type Simple = JsonProvider<"NugetRegistrationJsonSample.json">

    let GetLatestVersion (packagename: string) =
        try
            let url = sprintf "https://api.nuget.org/v3/registration1/%s/index.json" (packagename.ToLower())
            let response = Http.RequestString(url)
            let latestVersion = Simple.Parse(response).Items.[0].Upper
            Some latestVersion
        with    
            | _ -> None


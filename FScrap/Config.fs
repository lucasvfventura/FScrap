module FScrap.Config

open FSharp.Configuration
open System.IO

type SettingsType = YamlConfig<"Config.yaml">

let settings:SettingsType = 
    let default_settings = SettingsType()
    match File.Exists "FScrap.yaml" with
    | true -> 
        printfn "Load new settings"
        default_settings.Load "FScrap.yaml"
    | false -> 
        printfn "Using default settings"

    default_settings
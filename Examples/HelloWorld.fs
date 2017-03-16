open FScrap.Request

open System
open System.Net

type Item = {uri: Uri option ; html:string}

let processor (response: HttpWebResponse option) = async {
    match response with
    | Some(resp) ->
        use stream = resp.GetResponseStream()
        use reader = new IO.StreamReader(stream)
        return ({uri= Some(resp.ResponseUri); html = reader.ReadToEnd()} :> obj)
    | None ->
        return ({uri=None; html = "No response"} :> obj)
}

let pipeline (item:obj) = async {
    printfn "Starting the pipeline process"
    return item
}

let pipeline2 (item:obj) = async {
    let my_item = item :?> Item
    match my_item.uri with
    | Some(uri) ->
        printfn "%s" (uri.ToString())
    | None ->
        printfn "No uri"

    return item
}

let my_pipeline = [pipeline; pipeline2]

[<EntryPoint>]
let main argv = 
    [for uri in ["http://www.bing.com"; "http://www.google.com"; "http://www.microsoft.com"; "http://www.amazon.com"; "http://www.yahoo.com"] do
        for i in 1..4 ->
            Uri uri]
    |> List.map (fun uri -> request uri processor my_pipeline)
    |> ignore
    
    Console.ReadLine() |> ignore
    0

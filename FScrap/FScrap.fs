module FScrap.Request

open System
open System.Net
open FScrap.Config

type private RequestHtml = {
    uri:Uri; 
    processor:HttpWebResponse option -> Async<obj>; 
    pipeline:(obj->Async<obj>) list
}

let private fetchUrlAsync (uri:Uri) = async {
        let request = WebRequest.Create(uri) :?> HttpWebRequest
        request.UserAgent <- settings.FScrap.UserAgent
        request.Timeout <- settings.FScrap.RequestTimeout
        try
            let! response = request.AsyncGetResponse()
            let httpResponse = response :?> HttpWebResponse
            return Some(httpResponse)
        with :? WebException ->
            return None
    }

let private runPipeline pipeline item = 
    pipeline |> List.fold (fun item step -> async.Bind(item, step)) item        
        
let private processHtmlResponse request response = async {
    let item = request.processor response
    let! processedItem = runPipeline request.pipeline item
    return processedItem
}

let private createDownloadActor () =
    MailboxProcessor<RequestHtml>.Start <| fun self -> 
        let rec downloadUrl (previousRequestDate:DateTime) = async {
            let! request = self.Receive()
            let timeBetweenCalls =int <| Math.Ceiling (DateTime.UtcNow - previousRequestDate).TotalMilliseconds

            match settings.FScrap.RequestDelayPerDomain - timeBetweenCalls with
            | delay when delay > 0 -> do! Async.Sleep delay
            | _ -> ()

            let! response = fetchUrlAsync request.uri
            do! Async.StartChild (processHtmlResponse request response) |> Async.Ignore
            return! downloadUrl DateTime.UtcNow
        }
        downloadUrl DateTime.UtcNow

let private requester =
    MailboxProcessor.Start <| fun self -> 
        let rec downloadUrl (domainDownloadActors: Map<string, MailboxProcessor<RequestHtml>>) = async {
            let! request = self.Receive()
            match domainDownloadActors.TryFind request.uri.Host with
            | Some actor -> 
                actor.Post request
                return! downloadUrl domainDownloadActors
            | None -> 
                let actor = createDownloadActor ()
                actor.Post request
                return! downloadUrl (domainDownloadActors.Add (request.uri.Host, actor))
        }
        downloadUrl Map.empty

let request uri processor pipeline =
    requester.Post({uri=uri; processor=processor; pipeline=pipeline})
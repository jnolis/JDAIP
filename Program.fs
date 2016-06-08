namespace IpMonitor

open System.Net
open System.Text.RegularExpressions
open Newtonsoft.Json
open Tweetinvi

module SharedCode =
    let getUpLocation (levels:int) = 
        let upString =  Seq.replicate levels @"..\"
                        |> Seq.fold (+) @"\"
        let currentLocation = try Some (System.IO.Path.GetFullPath((new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath)) with
                                | :? System.NotSupportedException -> None
        match currentLocation with
                    | Some c ->   c
                                |> System.IO.Path.GetFullPath
                                |> (fun x-> x + upString)
                                |> System.IO.Path.GetFullPath
                    | None -> ""

    let solutionLocation () = getUpLocation 3

module Twitter = 
    let getCredentials () = 
        System.IO.Path.GetFullPath (SharedCode.solutionLocation() + @"Keys.json")
        |> System.IO.File.ReadAllText
        |> (fun x -> Newtonsoft.Json.JsonConvert.DeserializeObject<Core.Authentication.TwitterCredentials> (x))
 
    let setCredentials () =
        let credentials = getCredentials()
        do Auth.SetUserCredentials(credentials.ConsumerKey,credentials.ConsumerSecret,credentials.AccessToken,credentials.AccessTokenSecret)
            |> ignore
        do Auth.ApplicationCredentials <- credentials

    do setCredentials()
    let sendTweet(text:string) = Tweet.PublishTweet(text) |> ignore
        
module Ip = 
    let private webClient = new System.Net.WebClient()
    let private regex = new Regex("\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")
    let getIpAddress () = 
        "http://checkip.dyndns.org/"
        |> webClient.DownloadString
        |> regex.Match
        |> (fun x -> x.Value)
    
module Program =    
    [<EntryPoint>]
    let main (args) =
        do Seq.initInfinite id
            |> Seq.iter (fun x ->         
                do Twitter.sendTweet (Ip.getIpAddress())
                do System.Threading.Thread.Sleep (3600*1000))
        0

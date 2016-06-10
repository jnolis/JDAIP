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


module Ip = 
    let private webClient = new System.Net.WebClient()
    let private regex = new Regex("\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")
    let getIpAddress () = 
        try
            "http://checkip.dyndns.org/"
            |> webClient.DownloadString
            |> regex.Match
            |> (fun x -> 
                let address = x.Value
                if System.String.IsNullOrEmpty address then None else Some address)
        with | _ -> None
    let makeMessage () = 
        match getIpAddress () with
        | Some address -> "Address as of " + System.DateTime.Now.ToString() + ": " + address
        | None -> "Failed to get IP address at " + System.DateTime.Now.ToString()


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
        

    let startDmListener () =
        let eventFunction (eventArgs:Tweetinvi.Core.Events.EventArguments.MessageEventArgs) : unit =
            do Message.PublishMessage(Ip.makeMessage(),eventArgs.Message.SenderId) |> ignore
            System.Diagnostics.Debug.Write ("Recieved a DM from " + eventArgs.Message.SenderScreenName + ": " + eventArgs.Message.Text)

        let stream = Tweetinvi.Stream.CreateUserStream()
        do stream.MessageReceived.Add(eventFunction)

        do async { 
                let credentials = getCredentials()
                do stream.Credentials <- credentials
                do stream.StartStream()
                return ()
                }
        |> Async.Start
        |> ignore

    
module Program =    
    [<EntryPoint>]
    let main (args) =
        do Twitter.startDmListener()
        do Seq.initInfinite id
            |> Seq.iter (fun x ->
                async {
                    do Twitter.sendTweet (Ip.makeMessage())
                    return()
                }
                |> Async.Start
                |> ignore
                do System.Threading.Thread.Sleep (6*3600*1000))
        0

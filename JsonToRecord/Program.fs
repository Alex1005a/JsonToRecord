open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave
open Telegram.Bot
open Telegram.Bot.Types
open Telegram.Bot.Types.Enums
open Newtonsoft.Json
open System
open NovelFS.NovelIO


let client = new TelegramBotClient(Environment.GetEnvironmentVariable("botToken"))
client.SetWebhookAsync("https://76a49a8113da.ngrok.io/bot").Wait()

let parseUrl (url: string) =
    try 
        Some (new Uri(url))
    with _ -> 
        None

let parseUrlMessage (mes : string) = 
    let arr = mes.Split(' ')
    if arr.Length <> 3 then Error("Error parse message") 
    else match parseUrl arr.[1] with
         | Some url -> Ok(url, arr.[2])
         | None -> Error("Parse url")

let tryGetStringFromUrl (url : Uri) =
    try 
        let httpClient = new System.Net.Http.HttpClient()
        let response = httpClient.GetAsync(url).Result 
        response.Content.ReadAsStringAsync().Result |> Some
    with _ -> 
        None

let sendTextToChat (id : ChatId) (mes : string) = 
    io { client.SendTextMessageAsync(id, mes).Result |> ignore }

let browse =
    request (fun r ->
        let update = r.rawForm |> System.Text.Encoding.Default.GetString |> JsonConvert.DeserializeObject<Update>
        if update.Message.Type = MessageType.Text then
            match update.Message.Text with
            | "/start" -> IO.run (sendTextToChat (new ChatId(update.Message.Chat.Id)) "Bot start")
            | x when x.StartsWith("/url") -> 
                  match parseUrlMessage x with
                  | Result.Ok(url, name) -> 
                             match tryGetStringFromUrl url with
                             | Some json -> IO.run (sendTextToChat 
                                                   (new ChatId(update.Message.Chat.Id)) (JsonModule.jsonToRecord json name))
                             | None -> IO.run (sendTextToChat (new ChatId(update.Message.Chat.Id)) "Url don't return json")
                  | Result.Error(e) -> IO.run (sendTextToChat (new ChatId(update.Message.Chat.Id)) e)
            | _ -> IO.run (sendTextToChat (new ChatId(update.Message.Chat.Id)) "?")
                     
        OK "OK" 
        )

let webPart =    
    choose [ path "/bot" >=> browse ]


[<EntryPoint>]
let main _ =
    startWebServer defaultConfig webPart
    0 

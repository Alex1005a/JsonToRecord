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
client.SetWebhookAsync("https://fe1962dca5c2.ngrok.io/bot").Wait() // For test

let parseUrl (url: string) =
    try 
        Some (new Uri(url))
    with _ -> 
        None

let parseUrlMessage (message : string) = 
    let messageeWords = message.Split(' ')
    if messageeWords.Length <> 3 then Error("Error parse message") 
    else match parseUrl messageeWords.[1] with
         | Some url -> Ok(url, messageeWords.[2])
         | None -> Error("Parse url")

let tryGetStringFromUrl (url : Uri) =
    try 
        let httpClient = new System.Net.Http.HttpClient()
        let response = httpClient.GetAsync(url).Result 
        response.Content.ReadAsStringAsync().Result |> Ok
    with _ -> 
        Error("Url don't return json")

let sendTextToChat (id : ChatId) (message : string) = 
    io { client.SendTextMessageAsync(id, message).Result |> ignore }

let replyToCorrectMessage (chatId : ChatId) (correctMessage : string) =
    let result = correctMessage |> parseUrlMessage 
                 |> Result.bind(fun (url, name) -> tryGetStringFromUrl url |> Result.map(fun stringBody -> (stringBody, name) ))
    match result with
    | Result.Ok(json, name) -> sendTextToChat chatId (JsonModule.jsonToRecord json name)
    | Result.Error(error) -> sendTextToChat chatId error

let telegramRequest request = io {
    let update = request.rawForm |> System.Text.Encoding.Default.GetString |> JsonConvert.DeserializeObject<Update>
    let chatId = new ChatId(update.Message.Chat.Id)

    if update.Message.Type = MessageType.Text then
        match update.Message.Text with
        | "/start" -> do! sendTextToChat chatId "Bot start"
        | message when message.StartsWith("/url") -> do! replyToCorrectMessage chatId message 
        | _ -> do! sendTextToChat chatId "?"
                 
    return OK "OK"
}

let browse =
    request (fun r -> IO.run(telegramRequest r))

let webPart =    
    choose [ path "/bot" >=> browse ]


[<EntryPoint>]
let main _ =
    startWebServer defaultConfig webPart
    0 

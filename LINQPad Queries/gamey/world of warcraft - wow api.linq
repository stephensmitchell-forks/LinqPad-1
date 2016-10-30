<Query Kind="FSharpProgram">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Microsoft.FSharp</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

//following https://dev.battle.net/io-docs
let showImages = true

let doLaters = ResizeArray<_>()
let dumpBlacklist blacklist t x = x.Dump(description=t,exclude=blacklist)
let prettifyJsonObj o = JsonConvert.SerializeObject(o, Formatting.Indented)
let prettifyJson s = s |> JsonConvert.DeserializeObject |> prettifyJsonObj
let deserialize<'t> s = JsonConvert.DeserializeObject<'t>(s) 
let deserializePartial propName s = Newtonsoft.Json.Linq.JObject.Parse(s).[propName]
module HttpClient = 
    let tryGetUrl headers (url:string) = 
        use hc = new System.Net.Http.HttpClient()
        hc.DefaultRequestHeaders.Add("Accept", "application/json")
        
        try
            hc.GetStringAsync(url).Result
            |> Some
        with ex ->
            url.Dump()
            ex.Dump()
            None
            
    let getToken key secret =
        tryGetUrl ["client_id",key; "client_secret", secret; "grant_type", "client_credentials"] 
    
module BattleNet =
    let url = "https://us.api.battle.net/"
    let getSavedApiKey() = Util.GetPassword("Diablo3ApiKey")
    let getSavedAccessToken() = Util.GetPassword("Diablo3AccessToken")
    (* 
        flow: get api key/secret
        Authorize -> Auth code
        GetAccessToken -> access token
    *)

module Warcraft =
    open Newtonsoft.Json
    type Quality = 
        |Poor
        |Common
        |Uncommon
        |Rare
        |Legendary
        
    type RaceRaw = {Id:int; Mask:int; Side:string; Name:string}
    type PetTypeRaw = {Id:int; Key:string; Name:string; TypeAbilityId: int; StrongAgainstId:int; WeakAgainstId: int}
    
    module RaceData = 
        type RaceSummary = { Races: RaceRaw list}
        let races = 
            sprintf "%swow/data/character/races?locale=en_US&apikey=%s" BattleNet.url (BattleNet.getSavedApiKey())
            |> HttpClient.tryGetUrl List.empty 
            |> Option.map (deserialize<RaceSummary> >> fun x -> x.Races)
            |> Option.get
    module PetTypeData =
        type PetTypeSummary = {PetTypes:PetTypeRaw list}
        
        let petTypes = 
            sprintf "%swow/data/pet/types?local=en_US&apikey=%s" BattleNet.url (BattleNet.getSavedApiKey())
            |> HttpClient.tryGetUrl List.empty
            |> Option.map (deserialize<PetTypeSummary> >> fun x -> x.PetTypes)
            |> Option.get
            
    PetTypeData.petTypes.Dump()
    type PetDisplay = {Name:String; Level:int; PetQuality:string; Power:int; Speed:int; IsFavorite:bool; Creature:string; CanBattle:bool; Raw:obj }
    
        
    type CharacterInfo = {LastModified: System.Int64; Name:string; Realm:string; Battlegroup:string; Class:int; Race:int; Gender:string; Level:int; AchievementPoints:int; PetsCollected:int; PetsNotCollected:int}
    module JsonWowDeserialization = 
        type PetStats = {Level:int; PetQualityId:int; Power:int; Speed:int;}
        type PetInfo = {Name:string; Stats:PetStats; IsFavorite:bool; CreatureName:string; CanBattle:bool; QualityId:int; IsFirstAbilitySlotSelected:bool; IsSecondAbilitySlotSelected:bool; IsThirdAbilitySlotSelected:bool  }
        type PetSummary= { NumCollected:int; NotCollected:int; Collected: PetInfo list}
        //thumbnails: worked: http://render-api-us.worldofwarcraft.com/static-render/us/rexxar/188/39460796-avatar.jpg
        let getThumbnailUrl thumbnailFromJson =
            sprintf "http://render-api-us.worldofwarcraft.com/static-render/us/%s" thumbnailFromJson //rexxar/188/39460796-avatar.jpg
        type CharacterRequestInfo = { LastModified: System.Int64; Name:string; Realm:string; Battlegroup:string; Class:int; Race:int; Gender:bool; Level:int; AchievementPoints:int; Thumbnail:string; CalcClass:string; Faction:int; Pets: PetSummary}
        let mapCharacterRequestInfo raw (x:CharacterRequestInfo) = 
            let mapQuality = 
                function
                | 0 -> Poor
                | 1 -> Common
                | 2 -> Uncommon
                | 3 -> Rare
                | 4 -> Legendary
                | x -> failwithf "Unexpected quality: %i" x
                >> sprintf "%A" 
                
            let petDisplays = 
                x.Pets.Collected 
                |> List.map (fun pi -> 
                                    if pi.QualityId <> pi.Stats.PetQualityId then
                                        doLaters.Add(fun () ->
                                            pi.Dump("quality <> Stats.PetQualityId")
                                        )
                                    {
                                        Name=pi.Name
                                        IsFavorite= pi.IsFavorite
                                        Creature=pi.CreatureName
                                        CanBattle=pi.CanBattle
                                        Level=pi.Stats.Level
                                        PetQuality=mapQuality pi.Stats.PetQualityId
                                        Power=pi.Stats.Power
                                        Speed=pi.Stats.Speed; Raw= Util.OnDemand("Raw",fun () -> raw) })
            let thumbnailUrl = getThumbnailUrl x.Thumbnail
            let thumbnail:obj = if showImages then Util.RawHtml(sprintf "<img src=\"%s\" />" thumbnailUrl) else thumbnailUrl |> box
            {
                CharacterInfo.LastModified = x.LastModified
                Name= x.Name
                Realm= x.Realm
                Battlegroup= x.Battlegroup
                Class= x.Class
                Race = x.Race
                Gender=if x.Gender then "F" else "M"
                Level = x.Level
                AchievementPoints= x.AchievementPoints
                PetsCollected = x.Pets.NumCollected
                PetsNotCollected = x.Pets.NotCollected
            }, thumbnail, petDisplays
        
        let deserializeCri s = 
            JsonConvert.DeserializeObject<CharacterRequestInfo>(s) 
            |> mapCharacterRequestInfo s
            
    open BattleNet
    
    let getPets (key:string) (realm:string) (characterName:string) (fields:string) (locale:string) (jsonp:string) =
        let url = sprintf "%swow/character/%s/%s?fields=pets&locale=%s&apikey=%s" BattleNet.url realm characterName locale key
        url
        |> HttpClient.tryGetUrl List.empty
open BattleNet        
open Warcraft

    
let rawPetData = Util.Cache(Func<_>(fun _ -> Warcraft.getPets (getSavedApiKey()) "rexxar" "mmsheep" "pets" "en-US" null))

rawPetData
|> Option.map JsonWowDeserialization.deserializeCri
|> Option.iter (Dump >> ignore)

rawPetData
|> Option.map prettifyJson
|> Dump
|> ignore
doLaters
|> Seq.iter(fun f-> f())
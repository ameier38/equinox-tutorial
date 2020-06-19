namespace Reactor

open Shared

type SeqConfig =
    { Url: string } with
    static member Load() =
        let scheme = Env.getEnv "SEQ_SCHEME" "http"
        let host = Env.getEnv "SEQ_HOST" "localhost" 
        let port = Env.getEnv "SEQ_PORT" "5341"
        { Url = sprintf "%s://%s:%s" scheme host port }

type MongoConfig =
    { Url: string
      Database: string } with
    static member Load(secretsDir:string) =
        let getMongoSecret = Env.getSecret secretsDir "mongo"
        let host = getMongoSecret "host" "MONGO_HOST" "localhost"
        let port = getMongoSecret "port" "MONGO_PORT" "27017"
        let user = getMongoSecret "user" "MONGO_USER" "admin"
        let password = getMongoSecret "password" "MONGO_PASSWORD" "changeit"
        let url = sprintf "mongodb://%s:%s@%s:%s?retryWrites=false" user password host port
        let database = getMongoSecret "database" "MONGO_DATABASE" "dealership"
        { Url = url
          Database = database }

type EventStoreConfig =
    { Url: string
      User: string
      Password: string } with
    static member Load(secretsDir:string) =
        let getEventStoreSecret = Env.getSecret secretsDir "eventstore"
        let scheme = getEventStoreSecret "scheme" "EVENTSTORE_SCHEME" "tcp"
        let host = getEventStoreSecret "host" "EVENTSTORE_HOST" "localhost"
        let port = getEventStoreSecret "port" "EVENTSTORE_PORT" "1113"
        let user = getEventStoreSecret "user" "EVENTSTORE_USER" "admin"
        let password = getEventStoreSecret "password" "EVENTSTORE_PASSWORD" "changeit"
        let url = sprintf "%s://%s:%s" scheme host port
        { Url = url
          User = user
          Password = password }
        
type Config =
    { AppName: string
      Debug: bool
      SeqConfig: SeqConfig
      MongoConfig: MongoConfig
      EventStoreConfig: EventStoreConfig } with
    static member Load() =
        let secretsDir = Env.getEnv "SECRETS_DIR" "/var/secrets"
        { AppName = "Vehicle Reactor"
          Debug = (Env.getEnv "DEBUG" "false").ToLower() = "true"
          SeqConfig = SeqConfig.Load()
          MongoConfig = MongoConfig.Load(secretsDir)
          EventStoreConfig = EventStoreConfig.Load(secretsDir) }

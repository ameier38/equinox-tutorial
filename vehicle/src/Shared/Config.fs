namespace Shared

type ServerConfig =
    { Host: string
      Port: int } with
    static member Load() =
        { Host = Env.getEnv "SERVER_HOST" "0.0.0.0"
          Port = Env.getEnv "SERVER_PORT" "50051" |> int }

type SeqConfig =
    { Url: string } with
    static member Load() =
        let scheme = Env.getEnv "SEQ_SCHEME" "http"
        let host = Env.getEnv "SEQ_HOST" "localhost" 
        let port = Env.getEnv "SEQ_PORT" "5341"
        { Url = sprintf "%s://%s:%s" scheme host port }

type MongoConfig =
    { Url: string
      Host: string
      Port: int
      ReplicaSet: string
      User: string
      Password: string
      Database: string } with
    static member Load(secretsDir:string) =
        let getMongoSecret = Env.getSecret secretsDir "mongo"
        let host = getMongoSecret "host" "MONGO_HOST" "localhost"
        let port = getMongoSecret "port" "MONGO_PORT" "27017" |> int
        let replicaSet = getMongoSecret "replica-set" "MONGO_REPLICA_SET" "rs0"
        let user = getMongoSecret "user" "MONGO_USER" "admin"
        let password = getMongoSecret "password" "MONGO_PASSWORD" "changeit"
        let url = sprintf "mongodb://%s:%i" host port
        let database = getMongoSecret "database" "MONGO_DATABASE" "dealership"
        { Url = url
          Host = host
          Port = port
          ReplicaSet = replicaSet
          User = user
          Password = password
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

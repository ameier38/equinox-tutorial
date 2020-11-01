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
      User: Secret
      Password: Secret
      Database: string } with
    static member Load(secretsDir:string) =
        let secretName = Env.getEnv "MONGO_SECRET" "mongo"
        let getSecret = Env.getSecret secretsDir secretName
        let host = Env.getEnv "MONGO_HOST" "localhost"
        let port = Env.getEnv "MONGO_PORT" "27017" |> int
        let replicaSet = Env.getEnv "MONGO_REPLICA_SET" "rs0"
        let user = getSecret "user" "MONGO_USER" "admin"
        let password = getSecret "password" "MONGO_PASSWORD" "changeit"
        let database = Env.getEnv "MONGO_DATABASE" "dealership"
        let addresses =
            host.Split(",")
            |> Array.map (fun host -> sprintf "%s:%i" host port)
            |> String.concat ","
        let url = sprintf "mongodb://%s/?replicaSet=%s" addresses replicaSet
        { Url = url
          Host = host
          Port = port
          ReplicaSet = replicaSet
          User = user
          Password = password
          Database = database }

type EventStoreConfig =
    { Url: string
      User: Secret
      Password: Secret } with
    static member Load(secretsDir:string) =
        let secretName = Env.getEnv "EVENTSTORE_SECRET" "eventstore"
        let getSecret = Env.getSecret secretsDir secretName
        let scheme = Env.getEnv "EVENTSTORE_SCHEME" "tcp"
        let host = Env.getEnv "EVENTSTORE_HOST" "localhost"
        let port = Env.getEnv "EVENTSTORE_PORT" "1113"
        let user = getSecret "user" "EVENTSTORE_USER" "admin"
        let password = getSecret "password" "EVENTSTORE_PASSWORD" "changeit"
        let url = sprintf "%s://%s:%s" scheme host port
        { Url = url
          User = user
          Password = password }

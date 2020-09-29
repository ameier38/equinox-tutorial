// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: cosmicdealership/vehicle/v1/vehicle_query_service.proto
// </auto-generated>
#pragma warning disable 0414, 1591
#region Designer generated code

using grpc = global::Grpc.Core;

namespace CosmicDealership.Vehicle.V1 {
  /// <summary>
  /// Service to handle interactions with Vehicles.
  /// </summary>
  public static partial class VehicleQueryService
  {
    static readonly string __ServiceName = "cosmicdealership.vehicle.v1.VehicleQueryService";

    static readonly grpc::Marshaller<global::CosmicDealership.Vehicle.V1.ListVehiclesRequest> __Marshaller_cosmicdealership_vehicle_v1_ListVehiclesRequest = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::CosmicDealership.Vehicle.V1.ListVehiclesRequest.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::CosmicDealership.Vehicle.V1.ListVehiclesResponse> __Marshaller_cosmicdealership_vehicle_v1_ListVehiclesResponse = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::CosmicDealership.Vehicle.V1.ListVehiclesResponse.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesRequest> __Marshaller_cosmicdealership_vehicle_v1_ListAvailableVehiclesRequest = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesRequest.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesResponse> __Marshaller_cosmicdealership_vehicle_v1_ListAvailableVehiclesResponse = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesResponse.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::CosmicDealership.Vehicle.V1.GetVehicleRequest> __Marshaller_cosmicdealership_vehicle_v1_GetVehicleRequest = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::CosmicDealership.Vehicle.V1.GetVehicleRequest.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::CosmicDealership.Vehicle.V1.GetVehicleResponse> __Marshaller_cosmicdealership_vehicle_v1_GetVehicleResponse = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::CosmicDealership.Vehicle.V1.GetVehicleResponse.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::CosmicDealership.Vehicle.V1.GetAvailableVehicleRequest> __Marshaller_cosmicdealership_vehicle_v1_GetAvailableVehicleRequest = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::CosmicDealership.Vehicle.V1.GetAvailableVehicleRequest.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::CosmicDealership.Vehicle.V1.GetAvailableVehicleResponse> __Marshaller_cosmicdealership_vehicle_v1_GetAvailableVehicleResponse = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::CosmicDealership.Vehicle.V1.GetAvailableVehicleResponse.Parser.ParseFrom);

    static readonly grpc::Method<global::CosmicDealership.Vehicle.V1.ListVehiclesRequest, global::CosmicDealership.Vehicle.V1.ListVehiclesResponse> __Method_ListVehicles = new grpc::Method<global::CosmicDealership.Vehicle.V1.ListVehiclesRequest, global::CosmicDealership.Vehicle.V1.ListVehiclesResponse>(
        grpc::MethodType.Unary,
        __ServiceName,
        "ListVehicles",
        __Marshaller_cosmicdealership_vehicle_v1_ListVehiclesRequest,
        __Marshaller_cosmicdealership_vehicle_v1_ListVehiclesResponse);

    static readonly grpc::Method<global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesRequest, global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesResponse> __Method_ListAvailableVehicles = new grpc::Method<global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesRequest, global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesResponse>(
        grpc::MethodType.Unary,
        __ServiceName,
        "ListAvailableVehicles",
        __Marshaller_cosmicdealership_vehicle_v1_ListAvailableVehiclesRequest,
        __Marshaller_cosmicdealership_vehicle_v1_ListAvailableVehiclesResponse);

    static readonly grpc::Method<global::CosmicDealership.Vehicle.V1.GetVehicleRequest, global::CosmicDealership.Vehicle.V1.GetVehicleResponse> __Method_GetVehicle = new grpc::Method<global::CosmicDealership.Vehicle.V1.GetVehicleRequest, global::CosmicDealership.Vehicle.V1.GetVehicleResponse>(
        grpc::MethodType.Unary,
        __ServiceName,
        "GetVehicle",
        __Marshaller_cosmicdealership_vehicle_v1_GetVehicleRequest,
        __Marshaller_cosmicdealership_vehicle_v1_GetVehicleResponse);

    static readonly grpc::Method<global::CosmicDealership.Vehicle.V1.GetAvailableVehicleRequest, global::CosmicDealership.Vehicle.V1.GetAvailableVehicleResponse> __Method_GetAvailableVehicle = new grpc::Method<global::CosmicDealership.Vehicle.V1.GetAvailableVehicleRequest, global::CosmicDealership.Vehicle.V1.GetAvailableVehicleResponse>(
        grpc::MethodType.Unary,
        __ServiceName,
        "GetAvailableVehicle",
        __Marshaller_cosmicdealership_vehicle_v1_GetAvailableVehicleRequest,
        __Marshaller_cosmicdealership_vehicle_v1_GetAvailableVehicleResponse);

    /// <summary>Service descriptor</summary>
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::CosmicDealership.Vehicle.V1.VehicleQueryServiceReflection.Descriptor.Services[0]; }
    }

    /// <summary>Base class for server-side implementations of VehicleQueryService</summary>
    [grpc::BindServiceMethod(typeof(VehicleQueryService), "BindService")]
    public abstract partial class VehicleQueryServiceBase
    {
      /// <summary>
      /// List vehicles.
      /// </summary>
      /// <param name="request">The request received from the client.</param>
      /// <param name="context">The context of the server-side call handler being invoked.</param>
      /// <returns>The response to send back to the client (wrapped by a task).</returns>
      public virtual global::System.Threading.Tasks.Task<global::CosmicDealership.Vehicle.V1.ListVehiclesResponse> ListVehicles(global::CosmicDealership.Vehicle.V1.ListVehiclesRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      /// <summary>
      /// List available vehicles.
      /// </summary>
      /// <param name="request">The request received from the client.</param>
      /// <param name="context">The context of the server-side call handler being invoked.</param>
      /// <returns>The response to send back to the client (wrapped by a task).</returns>
      public virtual global::System.Threading.Tasks.Task<global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesResponse> ListAvailableVehicles(global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      /// <summary>
      /// Get a vehicle.
      /// </summary>
      /// <param name="request">The request received from the client.</param>
      /// <param name="context">The context of the server-side call handler being invoked.</param>
      /// <returns>The response to send back to the client (wrapped by a task).</returns>
      public virtual global::System.Threading.Tasks.Task<global::CosmicDealership.Vehicle.V1.GetVehicleResponse> GetVehicle(global::CosmicDealership.Vehicle.V1.GetVehicleRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      /// <summary>
      /// Get an available vehicle.
      /// </summary>
      /// <param name="request">The request received from the client.</param>
      /// <param name="context">The context of the server-side call handler being invoked.</param>
      /// <returns>The response to send back to the client (wrapped by a task).</returns>
      public virtual global::System.Threading.Tasks.Task<global::CosmicDealership.Vehicle.V1.GetAvailableVehicleResponse> GetAvailableVehicle(global::CosmicDealership.Vehicle.V1.GetAvailableVehicleRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

    }

    /// <summary>Client for VehicleQueryService</summary>
    public partial class VehicleQueryServiceClient : grpc::ClientBase<VehicleQueryServiceClient>
    {
      /// <summary>Creates a new client for VehicleQueryService</summary>
      /// <param name="channel">The channel to use to make remote calls.</param>
      public VehicleQueryServiceClient(grpc::ChannelBase channel) : base(channel)
      {
      }
      /// <summary>Creates a new client for VehicleQueryService that uses a custom <c>CallInvoker</c>.</summary>
      /// <param name="callInvoker">The callInvoker to use to make remote calls.</param>
      public VehicleQueryServiceClient(grpc::CallInvoker callInvoker) : base(callInvoker)
      {
      }
      /// <summary>Protected parameterless constructor to allow creation of test doubles.</summary>
      protected VehicleQueryServiceClient() : base()
      {
      }
      /// <summary>Protected constructor to allow creation of configured clients.</summary>
      /// <param name="configuration">The client configuration.</param>
      protected VehicleQueryServiceClient(ClientBaseConfiguration configuration) : base(configuration)
      {
      }

      /// <summary>
      /// List vehicles.
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="headers">The initial metadata to send with the call. This parameter is optional.</param>
      /// <param name="deadline">An optional deadline for the call. The call will be cancelled if deadline is hit.</param>
      /// <param name="cancellationToken">An optional token for canceling the call.</param>
      /// <returns>The response received from the server.</returns>
      public virtual global::CosmicDealership.Vehicle.V1.ListVehiclesResponse ListVehicles(global::CosmicDealership.Vehicle.V1.ListVehiclesRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return ListVehicles(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      /// <summary>
      /// List vehicles.
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="options">The options for the call.</param>
      /// <returns>The response received from the server.</returns>
      public virtual global::CosmicDealership.Vehicle.V1.ListVehiclesResponse ListVehicles(global::CosmicDealership.Vehicle.V1.ListVehiclesRequest request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_ListVehicles, null, options, request);
      }
      /// <summary>
      /// List vehicles.
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="headers">The initial metadata to send with the call. This parameter is optional.</param>
      /// <param name="deadline">An optional deadline for the call. The call will be cancelled if deadline is hit.</param>
      /// <param name="cancellationToken">An optional token for canceling the call.</param>
      /// <returns>The call object.</returns>
      public virtual grpc::AsyncUnaryCall<global::CosmicDealership.Vehicle.V1.ListVehiclesResponse> ListVehiclesAsync(global::CosmicDealership.Vehicle.V1.ListVehiclesRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return ListVehiclesAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      /// <summary>
      /// List vehicles.
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="options">The options for the call.</param>
      /// <returns>The call object.</returns>
      public virtual grpc::AsyncUnaryCall<global::CosmicDealership.Vehicle.V1.ListVehiclesResponse> ListVehiclesAsync(global::CosmicDealership.Vehicle.V1.ListVehiclesRequest request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_ListVehicles, null, options, request);
      }
      /// <summary>
      /// List available vehicles.
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="headers">The initial metadata to send with the call. This parameter is optional.</param>
      /// <param name="deadline">An optional deadline for the call. The call will be cancelled if deadline is hit.</param>
      /// <param name="cancellationToken">An optional token for canceling the call.</param>
      /// <returns>The response received from the server.</returns>
      public virtual global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesResponse ListAvailableVehicles(global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return ListAvailableVehicles(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      /// <summary>
      /// List available vehicles.
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="options">The options for the call.</param>
      /// <returns>The response received from the server.</returns>
      public virtual global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesResponse ListAvailableVehicles(global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesRequest request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_ListAvailableVehicles, null, options, request);
      }
      /// <summary>
      /// List available vehicles.
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="headers">The initial metadata to send with the call. This parameter is optional.</param>
      /// <param name="deadline">An optional deadline for the call. The call will be cancelled if deadline is hit.</param>
      /// <param name="cancellationToken">An optional token for canceling the call.</param>
      /// <returns>The call object.</returns>
      public virtual grpc::AsyncUnaryCall<global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesResponse> ListAvailableVehiclesAsync(global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return ListAvailableVehiclesAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      /// <summary>
      /// List available vehicles.
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="options">The options for the call.</param>
      /// <returns>The call object.</returns>
      public virtual grpc::AsyncUnaryCall<global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesResponse> ListAvailableVehiclesAsync(global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesRequest request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_ListAvailableVehicles, null, options, request);
      }
      /// <summary>
      /// Get a vehicle.
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="headers">The initial metadata to send with the call. This parameter is optional.</param>
      /// <param name="deadline">An optional deadline for the call. The call will be cancelled if deadline is hit.</param>
      /// <param name="cancellationToken">An optional token for canceling the call.</param>
      /// <returns>The response received from the server.</returns>
      public virtual global::CosmicDealership.Vehicle.V1.GetVehicleResponse GetVehicle(global::CosmicDealership.Vehicle.V1.GetVehicleRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return GetVehicle(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      /// <summary>
      /// Get a vehicle.
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="options">The options for the call.</param>
      /// <returns>The response received from the server.</returns>
      public virtual global::CosmicDealership.Vehicle.V1.GetVehicleResponse GetVehicle(global::CosmicDealership.Vehicle.V1.GetVehicleRequest request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_GetVehicle, null, options, request);
      }
      /// <summary>
      /// Get a vehicle.
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="headers">The initial metadata to send with the call. This parameter is optional.</param>
      /// <param name="deadline">An optional deadline for the call. The call will be cancelled if deadline is hit.</param>
      /// <param name="cancellationToken">An optional token for canceling the call.</param>
      /// <returns>The call object.</returns>
      public virtual grpc::AsyncUnaryCall<global::CosmicDealership.Vehicle.V1.GetVehicleResponse> GetVehicleAsync(global::CosmicDealership.Vehicle.V1.GetVehicleRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return GetVehicleAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      /// <summary>
      /// Get a vehicle.
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="options">The options for the call.</param>
      /// <returns>The call object.</returns>
      public virtual grpc::AsyncUnaryCall<global::CosmicDealership.Vehicle.V1.GetVehicleResponse> GetVehicleAsync(global::CosmicDealership.Vehicle.V1.GetVehicleRequest request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_GetVehicle, null, options, request);
      }
      /// <summary>
      /// Get an available vehicle.
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="headers">The initial metadata to send with the call. This parameter is optional.</param>
      /// <param name="deadline">An optional deadline for the call. The call will be cancelled if deadline is hit.</param>
      /// <param name="cancellationToken">An optional token for canceling the call.</param>
      /// <returns>The response received from the server.</returns>
      public virtual global::CosmicDealership.Vehicle.V1.GetAvailableVehicleResponse GetAvailableVehicle(global::CosmicDealership.Vehicle.V1.GetAvailableVehicleRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return GetAvailableVehicle(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      /// <summary>
      /// Get an available vehicle.
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="options">The options for the call.</param>
      /// <returns>The response received from the server.</returns>
      public virtual global::CosmicDealership.Vehicle.V1.GetAvailableVehicleResponse GetAvailableVehicle(global::CosmicDealership.Vehicle.V1.GetAvailableVehicleRequest request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_GetAvailableVehicle, null, options, request);
      }
      /// <summary>
      /// Get an available vehicle.
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="headers">The initial metadata to send with the call. This parameter is optional.</param>
      /// <param name="deadline">An optional deadline for the call. The call will be cancelled if deadline is hit.</param>
      /// <param name="cancellationToken">An optional token for canceling the call.</param>
      /// <returns>The call object.</returns>
      public virtual grpc::AsyncUnaryCall<global::CosmicDealership.Vehicle.V1.GetAvailableVehicleResponse> GetAvailableVehicleAsync(global::CosmicDealership.Vehicle.V1.GetAvailableVehicleRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return GetAvailableVehicleAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      /// <summary>
      /// Get an available vehicle.
      /// </summary>
      /// <param name="request">The request to send to the server.</param>
      /// <param name="options">The options for the call.</param>
      /// <returns>The call object.</returns>
      public virtual grpc::AsyncUnaryCall<global::CosmicDealership.Vehicle.V1.GetAvailableVehicleResponse> GetAvailableVehicleAsync(global::CosmicDealership.Vehicle.V1.GetAvailableVehicleRequest request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_GetAvailableVehicle, null, options, request);
      }
      /// <summary>Creates a new instance of client from given <c>ClientBaseConfiguration</c>.</summary>
      protected override VehicleQueryServiceClient NewInstance(ClientBaseConfiguration configuration)
      {
        return new VehicleQueryServiceClient(configuration);
      }
    }

    /// <summary>Creates service definition that can be registered with a server</summary>
    /// <param name="serviceImpl">An object implementing the server-side handling logic.</param>
    public static grpc::ServerServiceDefinition BindService(VehicleQueryServiceBase serviceImpl)
    {
      return grpc::ServerServiceDefinition.CreateBuilder()
          .AddMethod(__Method_ListVehicles, serviceImpl.ListVehicles)
          .AddMethod(__Method_ListAvailableVehicles, serviceImpl.ListAvailableVehicles)
          .AddMethod(__Method_GetVehicle, serviceImpl.GetVehicle)
          .AddMethod(__Method_GetAvailableVehicle, serviceImpl.GetAvailableVehicle).Build();
    }

    /// <summary>Register service method with a service binder with or without implementation. Useful when customizing the  service binding logic.
    /// Note: this method is part of an experimental API that can change or be removed without any prior notice.</summary>
    /// <param name="serviceBinder">Service methods will be bound by calling <c>AddMethod</c> on this object.</param>
    /// <param name="serviceImpl">An object implementing the server-side handling logic.</param>
    public static void BindService(grpc::ServiceBinderBase serviceBinder, VehicleQueryServiceBase serviceImpl)
    {
      serviceBinder.AddMethod(__Method_ListVehicles, serviceImpl == null ? null : new grpc::UnaryServerMethod<global::CosmicDealership.Vehicle.V1.ListVehiclesRequest, global::CosmicDealership.Vehicle.V1.ListVehiclesResponse>(serviceImpl.ListVehicles));
      serviceBinder.AddMethod(__Method_ListAvailableVehicles, serviceImpl == null ? null : new grpc::UnaryServerMethod<global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesRequest, global::CosmicDealership.Vehicle.V1.ListAvailableVehiclesResponse>(serviceImpl.ListAvailableVehicles));
      serviceBinder.AddMethod(__Method_GetVehicle, serviceImpl == null ? null : new grpc::UnaryServerMethod<global::CosmicDealership.Vehicle.V1.GetVehicleRequest, global::CosmicDealership.Vehicle.V1.GetVehicleResponse>(serviceImpl.GetVehicle));
      serviceBinder.AddMethod(__Method_GetAvailableVehicle, serviceImpl == null ? null : new grpc::UnaryServerMethod<global::CosmicDealership.Vehicle.V1.GetAvailableVehicleRequest, global::CosmicDealership.Vehicle.V1.GetAvailableVehicleResponse>(serviceImpl.GetAvailableVehicle));
    }

  }
}
#endregion

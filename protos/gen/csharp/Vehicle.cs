// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: tutorial/vehicle/v1/vehicle.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Tutorial.Vehicle.V1 {

  /// <summary>Holder for reflection information generated from tutorial/vehicle/v1/vehicle.proto</summary>
  public static partial class VehicleReflection {

    #region Descriptor
    /// <summary>File descriptor for tutorial/vehicle/v1/vehicle.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static VehicleReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CiF0dXRvcmlhbC92ZWhpY2xlL3YxL3ZlaGljbGUucHJvdG8SE3R1dG9yaWFs",
            "LnZlaGljbGUudjEiNAoHVmVoaWNsZRIMCgRtYWtlGAIgASgJEg0KBW1vZGVs",
            "GAMgASgJEgwKBHllYXIYBCABKAUieQoMVmVoaWNsZVN0YXRlEi0KB3ZlaGlj",
            "bGUYASABKAsyHC50dXRvcmlhbC52ZWhpY2xlLnYxLlZlaGljbGUSOgoOdmVo",
            "aWNsZV9zdGF0dXMYAiABKA4yIi50dXRvcmlhbC52ZWhpY2xlLnYxLlZlaGlj",
            "bGVTdGF0dXMqhAEKDVZlaGljbGVTdGF0dXMSHgoaVkVISUNMRV9TVEFUVVNf",
            "VU5TUEVDSUZJRUQQABIcChhWRUhJQ0xFX1NUQVRVU19BVkFJTEFCTEUQARIa",
            "ChZWRUhJQ0xFX1NUQVRVU19SRU1PVkVEEAISGQoVVkVISUNMRV9TVEFUVVNf",
            "TEVBU0VEEANCK1oTdHV0b3JpYWwvdmVoaWNsZS92MaoCE1R1dG9yaWFsLlZl",
            "aGljbGUuVjFiBnByb3RvMw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(new[] {typeof(global::Tutorial.Vehicle.V1.VehicleStatus), }, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Tutorial.Vehicle.V1.Vehicle), global::Tutorial.Vehicle.V1.Vehicle.Parser, new[]{ "Make", "Model", "Year" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Tutorial.Vehicle.V1.VehicleState), global::Tutorial.Vehicle.V1.VehicleState.Parser, new[]{ "Vehicle", "VehicleStatus" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Enums
  public enum VehicleStatus {
    [pbr::OriginalName("VEHICLE_STATUS_UNSPECIFIED")] Unspecified = 0,
    [pbr::OriginalName("VEHICLE_STATUS_AVAILABLE")] Available = 1,
    [pbr::OriginalName("VEHICLE_STATUS_REMOVED")] Removed = 2,
    [pbr::OriginalName("VEHICLE_STATUS_LEASED")] Leased = 3,
  }

  #endregion

  #region Messages
  public sealed partial class Vehicle : pb::IMessage<Vehicle> {
    private static readonly pb::MessageParser<Vehicle> _parser = new pb::MessageParser<Vehicle>(() => new Vehicle());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<Vehicle> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Tutorial.Vehicle.V1.VehicleReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public Vehicle() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public Vehicle(Vehicle other) : this() {
      make_ = other.make_;
      model_ = other.model_;
      year_ = other.year_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public Vehicle Clone() {
      return new Vehicle(this);
    }

    /// <summary>Field number for the "make" field.</summary>
    public const int MakeFieldNumber = 2;
    private string make_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Make {
      get { return make_; }
      set {
        make_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "model" field.</summary>
    public const int ModelFieldNumber = 3;
    private string model_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Model {
      get { return model_; }
      set {
        model_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "year" field.</summary>
    public const int YearFieldNumber = 4;
    private int year_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int Year {
      get { return year_; }
      set {
        year_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as Vehicle);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(Vehicle other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Make != other.Make) return false;
      if (Model != other.Model) return false;
      if (Year != other.Year) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Make.Length != 0) hash ^= Make.GetHashCode();
      if (Model.Length != 0) hash ^= Model.GetHashCode();
      if (Year != 0) hash ^= Year.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (Make.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(Make);
      }
      if (Model.Length != 0) {
        output.WriteRawTag(26);
        output.WriteString(Model);
      }
      if (Year != 0) {
        output.WriteRawTag(32);
        output.WriteInt32(Year);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Make.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Make);
      }
      if (Model.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Model);
      }
      if (Year != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(Year);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(Vehicle other) {
      if (other == null) {
        return;
      }
      if (other.Make.Length != 0) {
        Make = other.Make;
      }
      if (other.Model.Length != 0) {
        Model = other.Model;
      }
      if (other.Year != 0) {
        Year = other.Year;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 18: {
            Make = input.ReadString();
            break;
          }
          case 26: {
            Model = input.ReadString();
            break;
          }
          case 32: {
            Year = input.ReadInt32();
            break;
          }
        }
      }
    }

  }

  public sealed partial class VehicleState : pb::IMessage<VehicleState> {
    private static readonly pb::MessageParser<VehicleState> _parser = new pb::MessageParser<VehicleState>(() => new VehicleState());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<VehicleState> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Tutorial.Vehicle.V1.VehicleReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public VehicleState() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public VehicleState(VehicleState other) : this() {
      vehicle_ = other.vehicle_ != null ? other.vehicle_.Clone() : null;
      vehicleStatus_ = other.vehicleStatus_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public VehicleState Clone() {
      return new VehicleState(this);
    }

    /// <summary>Field number for the "vehicle" field.</summary>
    public const int VehicleFieldNumber = 1;
    private global::Tutorial.Vehicle.V1.Vehicle vehicle_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::Tutorial.Vehicle.V1.Vehicle Vehicle {
      get { return vehicle_; }
      set {
        vehicle_ = value;
      }
    }

    /// <summary>Field number for the "vehicle_status" field.</summary>
    public const int VehicleStatusFieldNumber = 2;
    private global::Tutorial.Vehicle.V1.VehicleStatus vehicleStatus_ = global::Tutorial.Vehicle.V1.VehicleStatus.Unspecified;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::Tutorial.Vehicle.V1.VehicleStatus VehicleStatus {
      get { return vehicleStatus_; }
      set {
        vehicleStatus_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as VehicleState);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(VehicleState other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(Vehicle, other.Vehicle)) return false;
      if (VehicleStatus != other.VehicleStatus) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (vehicle_ != null) hash ^= Vehicle.GetHashCode();
      if (VehicleStatus != global::Tutorial.Vehicle.V1.VehicleStatus.Unspecified) hash ^= VehicleStatus.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (vehicle_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(Vehicle);
      }
      if (VehicleStatus != global::Tutorial.Vehicle.V1.VehicleStatus.Unspecified) {
        output.WriteRawTag(16);
        output.WriteEnum((int) VehicleStatus);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (vehicle_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Vehicle);
      }
      if (VehicleStatus != global::Tutorial.Vehicle.V1.VehicleStatus.Unspecified) {
        size += 1 + pb::CodedOutputStream.ComputeEnumSize((int) VehicleStatus);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(VehicleState other) {
      if (other == null) {
        return;
      }
      if (other.vehicle_ != null) {
        if (vehicle_ == null) {
          Vehicle = new global::Tutorial.Vehicle.V1.Vehicle();
        }
        Vehicle.MergeFrom(other.Vehicle);
      }
      if (other.VehicleStatus != global::Tutorial.Vehicle.V1.VehicleStatus.Unspecified) {
        VehicleStatus = other.VehicleStatus;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            if (vehicle_ == null) {
              Vehicle = new global::Tutorial.Vehicle.V1.Vehicle();
            }
            input.ReadMessage(Vehicle);
            break;
          }
          case 16: {
            VehicleStatus = (global::Tutorial.Vehicle.V1.VehicleStatus) input.ReadEnum();
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code

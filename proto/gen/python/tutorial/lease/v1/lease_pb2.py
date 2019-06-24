# -*- coding: utf-8 -*-
# Generated by the protocol buffer compiler.  DO NOT EDIT!
# source: tutorial/lease/v1/lease.proto

import sys
_b=sys.version_info[0]<3 and (lambda x:x) or (lambda x:x.encode('latin1'))
from google.protobuf.internal import enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from google.protobuf import reflection as _reflection
from google.protobuf import symbol_database as _symbol_database
# @@protoc_insertion_point(imports)

_sym_db = _symbol_database.Default()


from google.protobuf import timestamp_pb2 as google_dot_protobuf_dot_timestamp__pb2
from google.type import date_pb2 as google_dot_type_dot_date__pb2
from google.type import money_pb2 as google_dot_type_dot_money__pb2


DESCRIPTOR = _descriptor.FileDescriptor(
  name='tutorial/lease/v1/lease.proto',
  package='tutorial.lease.v1',
  syntax='proto3',
  serialized_options=_b('\n\025com.tutorial.lease.v1B\nLeaseProtoP\001Z\007leasev1\242\002\003TLX\252\002\021Tutorial.Lease.V1\312\002\021Tutorial\\Lease\\V1'),
  serialized_pb=_b('\n\x1dtutorial/lease/v1/lease.proto\x12\x11tutorial.lease.v1\x1a\x1fgoogle/protobuf/timestamp.proto\x1a\x16google/type/date.proto\x1a\x17google/type/money.proto\"a\n\x08\x41sOfDate\x12.\n\nas_at_time\x18\x01 \x01(\x0b\x32\x1a.google.protobuf.Timestamp\x12%\n\nas_on_date\x18\x02 \x01(\x0b\x32\x11.google.type.Date\"\xaf\x01\n\x05Lease\x12\x10\n\x08lease_id\x18\x01 \x01(\t\x12\x0f\n\x07user_id\x18\x02 \x01(\t\x12%\n\nstart_date\x18\x03 \x01(\x0b\x32\x11.google.type.Date\x12(\n\rmaturity_date\x18\x04 \x01(\x0b\x32\x11.google.type.Date\x12\x32\n\x16monthly_payment_amount\x18\x05 \x01(\x0b\x32\x12.google.type.Money\"y\n\x08NewLease\x12\x0f\n\x07user_id\x18\x01 \x01(\t\x12(\n\rmaturity_date\x18\x03 \x01(\x0b\x32\x11.google.type.Date\x12\x32\n\x16monthly_payment_amount\x18\x04 \x01(\x0b\x32\x12.google.type.Money\"I\n\x07Payment\x12\x12\n\npayment_id\x18\x01 \x01(\t\x12*\n\x0epayment_amount\x18\x02 \x01(\x0b\x32\x12.google.type.Money\"\x9b\x01\n\nLeaseEvent\x12\x10\n\x08\x65vent_id\x18\x01 \x01(\x05\x12\x36\n\x12\x65vent_created_time\x18\x02 \x01(\x0b\x32\x1a.google.protobuf.Timestamp\x12/\n\x14\x65vent_effective_date\x18\x03 \x01(\x0b\x32\x11.google.type.Date\x12\x12\n\nevent_type\x18\x04 \x01(\t\"\x9b\x02\n\x10LeaseObservation\x12+\n\x10observation_date\x18\x01 \x01(\x0b\x32\x11.google.type.Date\x12\'\n\x05lease\x18\x02 \x01(\x0b\x32\x18.tutorial.lease.v1.Lease\x12+\n\x0ftotal_scheduled\x18\x03 \x01(\x0b\x32\x12.google.type.Money\x12&\n\ntotal_paid\x18\x04 \x01(\x0b\x32\x12.google.type.Money\x12&\n\namount_due\x18\x05 \x01(\x0b\x32\x12.google.type.Money\x12\x34\n\x0clease_status\x18\x06 \x01(\x0e\x32\x1e.tutorial.lease.v1.LeaseStatus*b\n\x0bLeaseStatus\x12\x18\n\x14LEASE_STATUS_INVALID\x10\x00\x12\x1c\n\x18LEASE_STATUS_OUTSTANDING\x10\x01\x12\x1b\n\x17LEASE_STATUS_TERMINATED\x10\x02\x42\\\n\x15\x63om.tutorial.lease.v1B\nLeaseProtoP\x01Z\x07leasev1\xa2\x02\x03TLX\xaa\x02\x11Tutorial.Lease.V1\xca\x02\x11Tutorial\\Lease\\V1b\x06proto3')
  ,
  dependencies=[google_dot_protobuf_dot_timestamp__pb2.DESCRIPTOR,google_dot_type_dot_date__pb2.DESCRIPTOR,google_dot_type_dot_money__pb2.DESCRIPTOR,])

_LEASESTATUS = _descriptor.EnumDescriptor(
  name='LeaseStatus',
  full_name='tutorial.lease.v1.LeaseStatus',
  filename=None,
  file=DESCRIPTOR,
  values=[
    _descriptor.EnumValueDescriptor(
      name='LEASE_STATUS_INVALID', index=0, number=0,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='LEASE_STATUS_OUTSTANDING', index=1, number=1,
      serialized_options=None,
      type=None),
    _descriptor.EnumValueDescriptor(
      name='LEASE_STATUS_TERMINATED', index=2, number=2,
      serialized_options=None,
      type=None),
  ],
  containing_type=None,
  serialized_options=None,
  serialized_start=1053,
  serialized_end=1151,
)
_sym_db.RegisterEnumDescriptor(_LEASESTATUS)

LeaseStatus = enum_type_wrapper.EnumTypeWrapper(_LEASESTATUS)
LEASE_STATUS_INVALID = 0
LEASE_STATUS_OUTSTANDING = 1
LEASE_STATUS_TERMINATED = 2



_ASOFDATE = _descriptor.Descriptor(
  name='AsOfDate',
  full_name='tutorial.lease.v1.AsOfDate',
  filename=None,
  file=DESCRIPTOR,
  containing_type=None,
  fields=[
    _descriptor.FieldDescriptor(
      name='as_at_time', full_name='tutorial.lease.v1.AsOfDate.as_at_time', index=0,
      number=1, type=11, cpp_type=10, label=1,
      has_default_value=False, default_value=None,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
    _descriptor.FieldDescriptor(
      name='as_on_date', full_name='tutorial.lease.v1.AsOfDate.as_on_date', index=1,
      number=2, type=11, cpp_type=10, label=1,
      has_default_value=False, default_value=None,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
  ],
  extensions=[
  ],
  nested_types=[],
  enum_types=[
  ],
  serialized_options=None,
  is_extendable=False,
  syntax='proto3',
  extension_ranges=[],
  oneofs=[
  ],
  serialized_start=134,
  serialized_end=231,
)


_LEASE = _descriptor.Descriptor(
  name='Lease',
  full_name='tutorial.lease.v1.Lease',
  filename=None,
  file=DESCRIPTOR,
  containing_type=None,
  fields=[
    _descriptor.FieldDescriptor(
      name='lease_id', full_name='tutorial.lease.v1.Lease.lease_id', index=0,
      number=1, type=9, cpp_type=9, label=1,
      has_default_value=False, default_value=_b("").decode('utf-8'),
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
    _descriptor.FieldDescriptor(
      name='user_id', full_name='tutorial.lease.v1.Lease.user_id', index=1,
      number=2, type=9, cpp_type=9, label=1,
      has_default_value=False, default_value=_b("").decode('utf-8'),
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
    _descriptor.FieldDescriptor(
      name='start_date', full_name='tutorial.lease.v1.Lease.start_date', index=2,
      number=3, type=11, cpp_type=10, label=1,
      has_default_value=False, default_value=None,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
    _descriptor.FieldDescriptor(
      name='maturity_date', full_name='tutorial.lease.v1.Lease.maturity_date', index=3,
      number=4, type=11, cpp_type=10, label=1,
      has_default_value=False, default_value=None,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
    _descriptor.FieldDescriptor(
      name='monthly_payment_amount', full_name='tutorial.lease.v1.Lease.monthly_payment_amount', index=4,
      number=5, type=11, cpp_type=10, label=1,
      has_default_value=False, default_value=None,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
  ],
  extensions=[
  ],
  nested_types=[],
  enum_types=[
  ],
  serialized_options=None,
  is_extendable=False,
  syntax='proto3',
  extension_ranges=[],
  oneofs=[
  ],
  serialized_start=234,
  serialized_end=409,
)


_NEWLEASE = _descriptor.Descriptor(
  name='NewLease',
  full_name='tutorial.lease.v1.NewLease',
  filename=None,
  file=DESCRIPTOR,
  containing_type=None,
  fields=[
    _descriptor.FieldDescriptor(
      name='user_id', full_name='tutorial.lease.v1.NewLease.user_id', index=0,
      number=1, type=9, cpp_type=9, label=1,
      has_default_value=False, default_value=_b("").decode('utf-8'),
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
    _descriptor.FieldDescriptor(
      name='maturity_date', full_name='tutorial.lease.v1.NewLease.maturity_date', index=1,
      number=3, type=11, cpp_type=10, label=1,
      has_default_value=False, default_value=None,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
    _descriptor.FieldDescriptor(
      name='monthly_payment_amount', full_name='tutorial.lease.v1.NewLease.monthly_payment_amount', index=2,
      number=4, type=11, cpp_type=10, label=1,
      has_default_value=False, default_value=None,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
  ],
  extensions=[
  ],
  nested_types=[],
  enum_types=[
  ],
  serialized_options=None,
  is_extendable=False,
  syntax='proto3',
  extension_ranges=[],
  oneofs=[
  ],
  serialized_start=411,
  serialized_end=532,
)


_PAYMENT = _descriptor.Descriptor(
  name='Payment',
  full_name='tutorial.lease.v1.Payment',
  filename=None,
  file=DESCRIPTOR,
  containing_type=None,
  fields=[
    _descriptor.FieldDescriptor(
      name='payment_id', full_name='tutorial.lease.v1.Payment.payment_id', index=0,
      number=1, type=9, cpp_type=9, label=1,
      has_default_value=False, default_value=_b("").decode('utf-8'),
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
    _descriptor.FieldDescriptor(
      name='payment_amount', full_name='tutorial.lease.v1.Payment.payment_amount', index=1,
      number=2, type=11, cpp_type=10, label=1,
      has_default_value=False, default_value=None,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
  ],
  extensions=[
  ],
  nested_types=[],
  enum_types=[
  ],
  serialized_options=None,
  is_extendable=False,
  syntax='proto3',
  extension_ranges=[],
  oneofs=[
  ],
  serialized_start=534,
  serialized_end=607,
)


_LEASEEVENT = _descriptor.Descriptor(
  name='LeaseEvent',
  full_name='tutorial.lease.v1.LeaseEvent',
  filename=None,
  file=DESCRIPTOR,
  containing_type=None,
  fields=[
    _descriptor.FieldDescriptor(
      name='event_id', full_name='tutorial.lease.v1.LeaseEvent.event_id', index=0,
      number=1, type=5, cpp_type=1, label=1,
      has_default_value=False, default_value=0,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
    _descriptor.FieldDescriptor(
      name='event_created_time', full_name='tutorial.lease.v1.LeaseEvent.event_created_time', index=1,
      number=2, type=11, cpp_type=10, label=1,
      has_default_value=False, default_value=None,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
    _descriptor.FieldDescriptor(
      name='event_effective_date', full_name='tutorial.lease.v1.LeaseEvent.event_effective_date', index=2,
      number=3, type=11, cpp_type=10, label=1,
      has_default_value=False, default_value=None,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
    _descriptor.FieldDescriptor(
      name='event_type', full_name='tutorial.lease.v1.LeaseEvent.event_type', index=3,
      number=4, type=9, cpp_type=9, label=1,
      has_default_value=False, default_value=_b("").decode('utf-8'),
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
  ],
  extensions=[
  ],
  nested_types=[],
  enum_types=[
  ],
  serialized_options=None,
  is_extendable=False,
  syntax='proto3',
  extension_ranges=[],
  oneofs=[
  ],
  serialized_start=610,
  serialized_end=765,
)


_LEASEOBSERVATION = _descriptor.Descriptor(
  name='LeaseObservation',
  full_name='tutorial.lease.v1.LeaseObservation',
  filename=None,
  file=DESCRIPTOR,
  containing_type=None,
  fields=[
    _descriptor.FieldDescriptor(
      name='observation_date', full_name='tutorial.lease.v1.LeaseObservation.observation_date', index=0,
      number=1, type=11, cpp_type=10, label=1,
      has_default_value=False, default_value=None,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
    _descriptor.FieldDescriptor(
      name='lease', full_name='tutorial.lease.v1.LeaseObservation.lease', index=1,
      number=2, type=11, cpp_type=10, label=1,
      has_default_value=False, default_value=None,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
    _descriptor.FieldDescriptor(
      name='total_scheduled', full_name='tutorial.lease.v1.LeaseObservation.total_scheduled', index=2,
      number=3, type=11, cpp_type=10, label=1,
      has_default_value=False, default_value=None,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
    _descriptor.FieldDescriptor(
      name='total_paid', full_name='tutorial.lease.v1.LeaseObservation.total_paid', index=3,
      number=4, type=11, cpp_type=10, label=1,
      has_default_value=False, default_value=None,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
    _descriptor.FieldDescriptor(
      name='amount_due', full_name='tutorial.lease.v1.LeaseObservation.amount_due', index=4,
      number=5, type=11, cpp_type=10, label=1,
      has_default_value=False, default_value=None,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
    _descriptor.FieldDescriptor(
      name='lease_status', full_name='tutorial.lease.v1.LeaseObservation.lease_status', index=5,
      number=6, type=14, cpp_type=8, label=1,
      has_default_value=False, default_value=0,
      message_type=None, enum_type=None, containing_type=None,
      is_extension=False, extension_scope=None,
      serialized_options=None, file=DESCRIPTOR),
  ],
  extensions=[
  ],
  nested_types=[],
  enum_types=[
  ],
  serialized_options=None,
  is_extendable=False,
  syntax='proto3',
  extension_ranges=[],
  oneofs=[
  ],
  serialized_start=768,
  serialized_end=1051,
)

_ASOFDATE.fields_by_name['as_at_time'].message_type = google_dot_protobuf_dot_timestamp__pb2._TIMESTAMP
_ASOFDATE.fields_by_name['as_on_date'].message_type = google_dot_type_dot_date__pb2._DATE
_LEASE.fields_by_name['start_date'].message_type = google_dot_type_dot_date__pb2._DATE
_LEASE.fields_by_name['maturity_date'].message_type = google_dot_type_dot_date__pb2._DATE
_LEASE.fields_by_name['monthly_payment_amount'].message_type = google_dot_type_dot_money__pb2._MONEY
_NEWLEASE.fields_by_name['maturity_date'].message_type = google_dot_type_dot_date__pb2._DATE
_NEWLEASE.fields_by_name['monthly_payment_amount'].message_type = google_dot_type_dot_money__pb2._MONEY
_PAYMENT.fields_by_name['payment_amount'].message_type = google_dot_type_dot_money__pb2._MONEY
_LEASEEVENT.fields_by_name['event_created_time'].message_type = google_dot_protobuf_dot_timestamp__pb2._TIMESTAMP
_LEASEEVENT.fields_by_name['event_effective_date'].message_type = google_dot_type_dot_date__pb2._DATE
_LEASEOBSERVATION.fields_by_name['observation_date'].message_type = google_dot_type_dot_date__pb2._DATE
_LEASEOBSERVATION.fields_by_name['lease'].message_type = _LEASE
_LEASEOBSERVATION.fields_by_name['total_scheduled'].message_type = google_dot_type_dot_money__pb2._MONEY
_LEASEOBSERVATION.fields_by_name['total_paid'].message_type = google_dot_type_dot_money__pb2._MONEY
_LEASEOBSERVATION.fields_by_name['amount_due'].message_type = google_dot_type_dot_money__pb2._MONEY
_LEASEOBSERVATION.fields_by_name['lease_status'].enum_type = _LEASESTATUS
DESCRIPTOR.message_types_by_name['AsOfDate'] = _ASOFDATE
DESCRIPTOR.message_types_by_name['Lease'] = _LEASE
DESCRIPTOR.message_types_by_name['NewLease'] = _NEWLEASE
DESCRIPTOR.message_types_by_name['Payment'] = _PAYMENT
DESCRIPTOR.message_types_by_name['LeaseEvent'] = _LEASEEVENT
DESCRIPTOR.message_types_by_name['LeaseObservation'] = _LEASEOBSERVATION
DESCRIPTOR.enum_types_by_name['LeaseStatus'] = _LEASESTATUS
_sym_db.RegisterFileDescriptor(DESCRIPTOR)

AsOfDate = _reflection.GeneratedProtocolMessageType('AsOfDate', (_message.Message,), dict(
  DESCRIPTOR = _ASOFDATE,
  __module__ = 'tutorial.lease.v1.lease_pb2'
  # @@protoc_insertion_point(class_scope:tutorial.lease.v1.AsOfDate)
  ))
_sym_db.RegisterMessage(AsOfDate)

Lease = _reflection.GeneratedProtocolMessageType('Lease', (_message.Message,), dict(
  DESCRIPTOR = _LEASE,
  __module__ = 'tutorial.lease.v1.lease_pb2'
  # @@protoc_insertion_point(class_scope:tutorial.lease.v1.Lease)
  ))
_sym_db.RegisterMessage(Lease)

NewLease = _reflection.GeneratedProtocolMessageType('NewLease', (_message.Message,), dict(
  DESCRIPTOR = _NEWLEASE,
  __module__ = 'tutorial.lease.v1.lease_pb2'
  # @@protoc_insertion_point(class_scope:tutorial.lease.v1.NewLease)
  ))
_sym_db.RegisterMessage(NewLease)

Payment = _reflection.GeneratedProtocolMessageType('Payment', (_message.Message,), dict(
  DESCRIPTOR = _PAYMENT,
  __module__ = 'tutorial.lease.v1.lease_pb2'
  # @@protoc_insertion_point(class_scope:tutorial.lease.v1.Payment)
  ))
_sym_db.RegisterMessage(Payment)

LeaseEvent = _reflection.GeneratedProtocolMessageType('LeaseEvent', (_message.Message,), dict(
  DESCRIPTOR = _LEASEEVENT,
  __module__ = 'tutorial.lease.v1.lease_pb2'
  # @@protoc_insertion_point(class_scope:tutorial.lease.v1.LeaseEvent)
  ))
_sym_db.RegisterMessage(LeaseEvent)

LeaseObservation = _reflection.GeneratedProtocolMessageType('LeaseObservation', (_message.Message,), dict(
  DESCRIPTOR = _LEASEOBSERVATION,
  __module__ = 'tutorial.lease.v1.lease_pb2'
  # @@protoc_insertion_point(class_scope:tutorial.lease.v1.LeaseObservation)
  ))
_sym_db.RegisterMessage(LeaseObservation)


DESCRIPTOR._options = None
# @@protoc_insertion_point(module_scope)

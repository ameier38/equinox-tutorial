module Shared.Dto

open MongoDB.Bson
open System

type InventoriedVehicleDto =
    { _id: ObjectId
      vehicleId: string
      addedAt: DateTimeOffset
      updatedAt: DateTimeOffset
      make: string
      model: string
      year: int
      status: string
      avatar: string
      images: string array }

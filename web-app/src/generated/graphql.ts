export type Maybe<T> = T | null;
/** All built-in and custom scalars, mapped to their actual values */
export type Scalars = {
  ID: string,
  String: string,
  Boolean: boolean,
  Int: number,
  Float: number,
  /** 
 * The `Date` scalar type represents a Date value with Time component. The Date
   * type appears in a JSON response as a String representation compatible with
   * ISO-8601 format.
 **/
  Date: any,
  /** 
 * The `URI` scalar type represents a string resource identifier compatible with
   * URI standard. The URI type appears in a JSON response as a String.
 **/
  URI: any,
};




/** Point in time on and at which to observe state */
export type AsOf = {
  /** Filter events created at or before this date */
  asAt?: Maybe<Scalars['String']>,
  /** Filter events effective on or before this date */
  asOn?: Maybe<Scalars['String']>,
};

/** Input for creating a lease */
export type CreateLeaseInput = {
  /** Unique identifier of the lease */
  leaseId: Scalars['ID'],
  /** Unique identifier of the user */
  userId: Scalars['ID'],
  /** Date on which the lease begins */
  commencementDate: Scalars['Date'],
  /** Date on which the lease ends */
  expirationDate: Scalars['Date'],
  /** Monthly payment amount for the lease */
  monthlyPaymentAmount: Scalars['Float'],
};


/** Input for deleting a lease event */
export type DeleteLeaseEventInput = {
  /** Unique identifier of a lease */
  leaseId: Scalars['ID'],
  eventId: Scalars['Int'],
};

export type GetLeaseInput = {
  /** Unique identifier of a lease */
  leaseId: Scalars['ID'],
  /** Point in time on and at which to observe state */
  asOf?: Maybe<AsOf>,
};

/** Static information for a lease */
export type Lease = {
   __typename?: 'Lease',
  commencementDate: Scalars['Date'],
  expirationDate: Scalars['Date'],
  leaseId: Scalars['ID'],
  monthlyPaymentAmount: Scalars['Float'],
  userId: Scalars['ID'],
};

/** Lease event that has occured */
export type LeaseEvent = {
   __typename?: 'LeaseEvent',
  eventCreatedTime: Scalars['Date'],
  eventEffectiveDate: Scalars['Date'],
  eventId: Scalars['Int'],
  eventPayload: Scalars['String'],
  eventType: Scalars['String'],
};

/** Observation of a lease as of a particular date */
export type LeaseObservation = {
   __typename?: 'LeaseObservation',
  amountDue: Scalars['Float'],
  commencementDate: Scalars['Date'],
  createdAtTime: Scalars['Date'],
  expirationDate: Scalars['Date'],
  leaseId: Scalars['ID'],
  leaseStatus: LeaseStatus,
  monthlyPaymentAmount: Scalars['Float'],
  totalPaid: Scalars['Float'],
  totalScheduled: Scalars['Float'],
  updatedAtTime: Scalars['Date'],
  updatedOnDate: Scalars['Date'],
  userId: Scalars['ID'],
};

/** Status of the lease */
export enum LeaseStatus {
  Outstanding = 'Outstanding',
  Terminated = 'Terminated'
}

/** Input for listing lease events */
export type ListLeaseEventsInput = {
  /** Unique identifier of a lease */
  leaseId: Scalars['ID'],
  /** Point in time on and at which to observe state */
  asOf?: Maybe<AsOf>,
  /** Maximum number of items in a page */
  pageSize?: Maybe<Scalars['Int']>,
  /** Token for page to retrieve; Empty string for first page */
  pageToken?: Maybe<Scalars['ID']>,
};

/** List lease events repsonse */
export type ListLeaseEventsResponse = {
   __typename?: 'ListLeaseEventsResponse',
  events: Array<LeaseEvent>,
  nextPageToken: Scalars['String'],
  prevPageToken: Scalars['String'],
  totalCount: Scalars['Int'],
};

export type ListLeasesInput = {
  /** Maximum number of items in a page */
  pageSize?: Maybe<Scalars['Int']>,
  /** Token for page to retrieve; Empty string for first page */
  pageToken?: Maybe<Scalars['ID']>,
};

/** List leases response */
export type ListLeasesResponse = {
   __typename?: 'ListLeasesResponse',
  leases: Array<Lease>,
  nextPageToken: Scalars['String'],
  prevPageToken: Scalars['String'],
  totalCount: Scalars['Int'],
};

export type Mutation = {
   __typename?: 'Mutation',
  /** Create a new lease */
  createLease: Scalars['String'],
  /** delete a lease event */
  deleteLeaseEvent: Scalars['String'],
  /** receive a payment */
  receivePayment: Scalars['String'],
  /** Schedule a payment */
  schedulePayment: Scalars['String'],
  /** terminate a lease */
  terminateLease: Scalars['String'],
};


export type MutationCreateLeaseArgs = {
  input: CreateLeaseInput
};


export type MutationDeleteLeaseEventArgs = {
  input: DeleteLeaseEventInput
};


export type MutationReceivePaymentArgs = {
  input: ReceivePaymentInput
};


export type MutationSchedulePaymentArgs = {
  input: SchedulePaymentInput
};


export type MutationTerminateLeaseArgs = {
  input: TerminateLeaseInput
};

export type Query = {
   __typename?: 'Query',
  /** Get a lease at a point in time */
  getLease: LeaseObservation,
  listLeaseEvents: ListLeaseEventsResponse,
  /** List existing leases */
  listLeases: ListLeasesResponse,
};


export type QueryGetLeaseArgs = {
  input: GetLeaseInput
};


export type QueryListLeaseEventsArgs = {
  input: ListLeaseEventsInput
};


export type QueryListLeasesArgs = {
  input: ListLeasesInput
};

/** Input for receiving a payment */
export type ReceivePaymentInput = {
  /** Unique identifier of a lease */
  leaseId: Scalars['ID'],
  paymentId: Scalars['ID'],
  receivedDate: Scalars['Date'],
  receivedAmount: Scalars['Float'],
};

/** Input for scheduling a payment */
export type SchedulePaymentInput = {
  /** Unique identifier of a lease */
  leaseId: Scalars['ID'],
  paymentId: Scalars['ID'],
  scheduledDate: Scalars['Date'],
  scheduledAmount: Scalars['Float'],
};

/** Input for terminating a lease */
export type TerminateLeaseInput = {
  /** Unique identifier of a lease */
  leaseId: Scalars['ID'],
  terminationDate: Scalars['Date'],
  terminationReason: Scalars['String'],
};


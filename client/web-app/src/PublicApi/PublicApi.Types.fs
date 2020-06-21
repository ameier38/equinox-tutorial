namespace rec PublicApi

type ListVehiclesInput =
    { /// Token for page to retrieve; Empty string for first page
      pageToken: Option<string>
      /// Maximum number of items in a page
      pageSize: Option<int> }

/// The error returned by the GraphQL backend
type ErrorType = { message: string }

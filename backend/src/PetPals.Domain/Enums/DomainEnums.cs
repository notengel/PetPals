namespace PetPals.Domain.Enums;

public enum UserRole
{
    User,
    Clinic,
    Shelter
}

public enum AppointmentStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Completed
}

public enum ProductCategory
{
    Food,
    Vaccine,
    Medicine,
    Accessory,
    Hygiene,
    Other
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Completed
}

public enum AdoptablePetStatus
{
    Available,
    InProcess,
    Adopted,
    Unavailable
}

public enum AdoptionRequestStatus
{
    Pending,
    UnderReview,
    Approved,
    Rejected,
    Cancelled
}

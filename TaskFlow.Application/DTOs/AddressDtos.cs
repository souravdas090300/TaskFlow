using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskFlow.Application.DTOs;

public class AddressDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty; // "Shipping" or "Billing"
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAddressDto
{
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = string.Empty; // "Shipping" or "Billing"

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string AddressLine1 { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? AddressLine2 { get; set; }

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string State { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    public bool IsDefault { get; set; }
}

public class UpdateAddressDto
{
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string AddressLine1 { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? AddressLine2 { get; set; }

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string State { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    public bool IsDefault { get; set; }
}

public class PatchAddressDto
{
    [JsonPropertyName("type")]
    [MaxLength(20)]
    public string? Type { get; set; }

    [JsonPropertyName("firstName")]
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    [MaxLength(100)]
    public string? LastName { get; set; }

    [JsonPropertyName("addressLine1")]
    [MaxLength(255)]
    public string? AddressLine1 { get; set; }

    [JsonPropertyName("addressLine2")]
    [MaxLength(255)]
    public string? AddressLine2 { get; set; }

    [JsonPropertyName("city")]
    [MaxLength(100)]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    [MaxLength(100)]
    public string? State { get; set; }

    [JsonPropertyName("postalCode")]
    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [JsonPropertyName("country")]
    [MaxLength(100)]
    public string? Country { get; set; }

    [JsonPropertyName("phone")]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [JsonPropertyName("isDefault")]
    public bool? IsDefault { get; set; }
}

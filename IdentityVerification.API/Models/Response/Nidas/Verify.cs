using System.Text.Json.Serialization;

namespace IdentityVerification.API.Models.Response.Nidas
{
    public class Verify
    {
        [JsonPropertyName("data")]
        public VerifyData Data { get; set; }

        [JsonPropertyName("meta")]
        public VerifyMeta Meta { get; set; }
    }

    public class VerifyData
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("reference")]
        public string Reference { get; set; }

        [JsonPropertyName("face_url")]
        public string FaceUrl { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("middle_name")]
        public string MiddleName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("suffix")]
        public string Suffix { get; set; }

        [JsonPropertyName("sex")]
        public string Sex { get; set; }

        [JsonPropertyName("marital_status")]
        public string MaritalStatus { get; set; }

        [JsonPropertyName("blood_type")]
        public string BloodType { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("mobile_number")]
        public string MobileNumber { get; set; }

        [JsonPropertyName("birth_date")]
        public string BirthDate { get; set; }

        [JsonPropertyName("full_address")]
        public string FullAddress { get; set; }

        [JsonPropertyName("address_line_1")]
        public string AddressLine1 { get; set; }

        [JsonPropertyName("address_line_2")]
        public string? AddressLine2 { get; set; } // Nullable

        [JsonPropertyName("barangay")]
        public string Barangay { get; set; }

        [JsonPropertyName("municipality")]
        public string Municipality { get; set; }

        [JsonPropertyName("province")]
        public string Province { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("postal_code")]
        public int PostalCode { get; set; }

        [JsonPropertyName("present_full_address")]
        public string PresentFullAddress { get; set; }

        [JsonPropertyName("present_address_line_1")]
        public string PresentAddressLine1 { get; set; }

        [JsonPropertyName("present_address_line_2")]
        public string? PresentAddressLine2 { get; set; } // Nullable

        [JsonPropertyName("present_barangay")]
        public string PresentBarangay { get; set; }

        [JsonPropertyName("present_municipality")]
        public string PresentMunicipality { get; set; }

        [JsonPropertyName("present_province")]
        public string PresentProvince { get; set; }

        [JsonPropertyName("present_country")]
        public string PresentCountry { get; set; }

        [JsonPropertyName("present_postal_code")]
        public int PresentPostalCode { get; set; }

        [JsonPropertyName("residency_status")]
        public string ResidencyStatus { get; set; }

        [JsonPropertyName("place_of_birth")]
        public string PlaceOfBirth { get; set; }

        [JsonPropertyName("pob_municipality")]
        public string PobMunicipality { get; set; }

        [JsonPropertyName("pob_province")]
        public string PobProvince { get; set; }

        [JsonPropertyName("pob_country")]
        public string PobCountry { get; set; }

        [JsonPropertyName("verified")]
        public bool? Verified { get; set; } //200 Failed Verification

    }

    public class VerifyMeta
    {

        [JsonPropertyName("result_grade")]
        public int? ResultGrade { get; set; }

        [JsonPropertyName("tier_level")]
        public string TierLevel { get; set; }

    }

}

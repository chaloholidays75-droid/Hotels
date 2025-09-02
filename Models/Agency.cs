using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace HotelAPI.Models
{
    public class Agency
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Agency Name is required")]
        [StringLength(100, ErrorMessage = "Agency Name cannot exceed 100 characters")]
        public string? AgencyName { get; set; }

        [Required(ErrorMessage = "Country is required")]
        public string? Country { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string? City { get; set; }

        [Required(ErrorMessage = "Post Code is required")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-]{3,10}$", ErrorMessage = "Please enter a valid post code")]
        public string? PostCode { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string? Address { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? Website { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        [RegularExpression(@"^[+]{0,1}[0-9\s\-\(\)]{8,15}$", ErrorMessage = "Please enter a valid phone number")]
        public string? PhoneNo { get; set; }

        [CustomEmailListValidation(ErrorMessage = "Please enter valid email addresses separated by commas")]
        public string? EmailId { get; set; }

        [Required(ErrorMessage = "Business Currency is required")]
        public string BusinessCurrency { get; set; } = "USD";

        public string? Title { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        [StringLength(50, ErrorMessage = "First Name cannot exceed 50 characters")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        [StringLength(50, ErrorMessage = "Last Name cannot exceed 50 characters")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string? UserEmailId { get; set; }

        [Required(ErrorMessage = "Designation is required")]
        [StringLength(50, ErrorMessage = "Designation cannot exceed 50 characters")]
        public string? Designation { get; set; }

        [Required(ErrorMessage = "Mobile Number is required")]
        [RegularExpression(@"^[+]{0,1}[0-9\s\-\(\)]{8,15}$", ErrorMessage = "Please enter a valid mobile number")]
        public string? MobileNo { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 30 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", 
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the Terms and Conditions")]
        public bool AcceptTerms { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
    }

    // Custom validation attribute for email lists
    public class CustomEmailListValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return ValidationResult.Success;

            var emails = value.ToString().Split(',');
            var emailAttribute = new EmailAddressAttribute();

            foreach (var email in emails)
            {
                if (!string.IsNullOrWhiteSpace(email) && !emailAttribute.IsValid(email.Trim()))
                {
                    return new ValidationResult(ErrorMessage ?? "Please enter valid email addresses separated by commas");
                }
            }

            return ValidationResult.Success;
        }
    }
}
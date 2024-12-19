using Database.DTOs;
using FluentValidation;

namespace eKids.Validators
{
    public class UpdateUserValidator : AbstractValidator<UpdateUser>
    {
        public UpdateUserValidator() {

            RuleFor(user => user.Password)
                .NotEmpty().WithMessage("Password is required.")
                .SetValidator(new PasswordValidator())
                .When(user => !string.IsNullOrEmpty(user.Password));

            RuleFor(user => user.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm Password is required.")
                .Equal(user => user.Password)
                .SetValidator(new PasswordValidator())
                .WithMessage("Passwords must match.")
                .When(user => !string.IsNullOrEmpty(user.Password)); 

            RuleFor(user => user.Email)
                .EmailAddress()
                .WithMessage("Valid email address required");
        }
    }
}

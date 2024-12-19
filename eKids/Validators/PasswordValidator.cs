using FluentValidation;

namespace eKids.Validators
{
    public class PasswordValidator : AbstractValidator<string>
    {
        public PasswordValidator() { 
            RuleFor(password => password)
                .MinimumLength(8)
                .WithMessage("Minimum 8 character length")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        }
    }
}

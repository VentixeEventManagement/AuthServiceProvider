using Presentation.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Presentation.Documentation;

public class SignUpForm_Example : IExamplesProvider<SignUpForm>
{
    public SignUpForm GetExamples()
    {
        return new SignUpForm
        {
            Email = "test.user@domain.com",
            Password = "BytMig123!",
            verified = true
        };
    }
}

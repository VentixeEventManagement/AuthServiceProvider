using Presentation.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Presentation.Documentation;

public class SignInForm_Example : IExamplesProvider<SignInForm>
{
    public SignInForm GetExamples()
    {
        return new SignInForm
        {
            Email = "test.user@domain.com",
            Password = "BytMig123!",
        };
    }
}

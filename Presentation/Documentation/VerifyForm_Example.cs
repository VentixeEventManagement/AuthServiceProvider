using Presentation.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Presentation.Documentation;

public class VerifyForm_Example : IExamplesProvider<VerifyForm>
{
    public VerifyForm GetExamples()
    {
        return new VerifyForm
        {
            Email = "test.user@domain.com",
            Code = "123456"
        };

    }
}

namespace Presentation.Models;

public class GetAccountResult
{
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
}

public class GetAccountResult<T> : GetAccountResult
{
    public T? Account { get; set; }
}

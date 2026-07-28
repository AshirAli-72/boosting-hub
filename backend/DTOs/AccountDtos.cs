namespace BoostingHub.backend.DTOs;

public class AccountDto
{
    public int Id { get; set; }
    public string AccountTitle { get; set; } = "";
    public string MobileNumber { get; set; } = "";
    public string Cnic { get; set; } = "";
    public string? AccountNumber { get; set; }
    public string? BankName { get; set; }
    public bool IsDefault { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
}

public class CreateAccountDto
{
    public string AccountTitle { get; set; } = "";
    public string MobileNumber { get; set; } = "";
    public string Cnic { get; set; } = "";
    public string? AccountNumber { get; set; }
    public string? BankName { get; set; }
    public bool IsDefault { get; set; }
}

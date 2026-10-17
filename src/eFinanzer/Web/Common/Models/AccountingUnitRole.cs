using System.Text.Json.Serialization;

namespace eFinanzer.Common.Models;

public enum AccountingUnitRole
{
    /// <summary>
    /// Finanzer
    /// </summary>
    FinanceOfficer,
    /// <summary>
    /// Kassenwart
    /// </summary>
    Treasurer,
    /// <summary>
    /// Finanzbevollmächtigte
    /// </summary>
    FinanceAuthorizedUser,
}
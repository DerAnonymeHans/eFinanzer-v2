using System.ComponentModel.DataAnnotations;
using eFinanzer.Common.Database.Models.Attachments;
using eFinanzer.Common.Models;

namespace eFinanzer.Common.Database.Models;


public record Receipt
{
    public int Id { get; init; }
    public required string Key { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    
    public DateTimeOffset CreatedAt { get; init; }
    public Type ReceiptType { get; init; }
    public PaymentType PaymentType { get; init; }
    
    public string? Signature1 { get; init; }
    public string? Signature2 { get; init; }
    
    public int FiscalYearId { get; init; }
    public FiscalYear? FiscalYear { get; init; }
    
    public List<ReceiptPosition>? Positions { get; set; }
    
    public List<ReceiptAttachment>? Attachments { get; set; }

    public enum Type
    {
        /// <summary>
        /// Normale Buchung
        /// </summary>
        Booking,
        /// <summary>
        /// Haushaltsrate
        /// </summary>
        BudgetInstallment,
        /// <summary>
        /// Übertrag für neues Haushaltsjahr
        /// </summary>
        CarryOver
    }

    
}
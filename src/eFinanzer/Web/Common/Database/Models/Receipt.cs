using System.ComponentModel.DataAnnotations;
using eFinanzer.Common.Database.Models.Attachments;
using eFinanzer.Common.Models;

namespace eFinanzer.Common.Database.Models;


public record Receipt
{
    public int Id { get; init; }
    public required string Key { get; init; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    
    public DateTimeOffset CreatedAt { get; init; }
    public DateOnly ReceiptAt { get; set; }
    public Type ReceiptType { get; set; }
    public PaymentType PaymentType { get; set; }
    
    public string? Signature1 { get; set; }
    public string? Signature2 { get; set; }
    
    /// <summary>
    /// The person who receives the money, Only set for negative receipts
    /// </summary>
    public ReceiverData? Receiver { get; set; }
    
    public int? BudgetPositionId { get; set; }
    public BudgetPosition? BudgetPosition { get; set; }
    
    [MaxLength(10)]
    public required string AccountingUnitKey { get; init; }
    public AccountingUnit? AccountingUnit { get; init; }
    
    [MaxLength(10)]
    public required string FiscalYearKey { get; init; }
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

    public record ReceiverData
    {
        public required string Name { get; init; }
        public required string Iban { get; init; }
        public required string Street { get; init; }
        public required string City { get; init; }
    }
}
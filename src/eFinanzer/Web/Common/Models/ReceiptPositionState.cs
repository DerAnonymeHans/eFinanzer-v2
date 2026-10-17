using eFinanzer.Common.Database.Models;

namespace eFinanzer.Common.Models;

public enum ReceiptPositionState
{
    /// <summary>
    /// Position amount is a planned value - no money was moved
    /// </summary>
    Planned,
    /// <summary>
    /// Position amount was spent but the <see cref="AccountingUnit"/>s balance has not changed because it was paid by
    /// someone else, who will receive a disbursement
    /// </summary>
    Spent,
    /// <summary>
    /// Position amount was spent by the <see cref="AccountingUnit"/>. Its balance changes by the position amount
    /// </summary>
    Disbursed
}
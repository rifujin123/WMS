using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class AssociationRule : BaseAuditableEntity
{
    [MaxLength(500)]
    public string Antecedent { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Consequent { get; set; } = string.Empty;

    public double Confidence { get; set; }
    public double Lift { get; set; }
}

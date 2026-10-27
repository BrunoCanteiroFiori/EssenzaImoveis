using System.ComponentModel;

namespace Backend.Infrastructure.Enums;

public enum SortDirection
{
    [Description("Crescente")]
    Ascending,

    [Description("Decrescente")]
    Descending,
}

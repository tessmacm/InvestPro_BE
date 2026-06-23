using System;
using System.Collections.Generic;
using System.Text;

namespace IMS.Core.Entities;

public class InvestmentInterest
{
    public int Id { get; set; }
    public string DisplayRange { get; set; } = string.Empty; // e.g., "$50,000 - $100,000"

    // Pro-Tip: Storing the raw numeric boundaries makes filtering/sorting investors incredibly easy later
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
}

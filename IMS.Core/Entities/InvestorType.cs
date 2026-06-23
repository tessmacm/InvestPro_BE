using System;
using System.Collections.Generic;
using System.Text;

namespace IMS.Core.Entities;

public class InvestorType
{
    public int Id { get; set; }
    public string Name { get; set; } = String.Empty;
    public string? Description { get; set; }
    //public IEnumerable<Investor>? Investors { get; set; }
}

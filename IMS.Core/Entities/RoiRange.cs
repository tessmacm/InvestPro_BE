using System;
using System.Collections.Generic;
using System.Text;

namespace IMS.Core.Entities;

public class RoiRange
{
    public int Id { get; set; }
    public decimal Percentage { get; set; }
    public string? DisplayLabel { get; set; }
    public string Status { get; set; } = "Active";
}

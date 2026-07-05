namespace IMS.API.DTOs.Investor;

public class LookUpItemDTO
{
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class LookUpCollectionDTO
{
    public List<LookUpItemDTO> InvestorTypes { get; set; } = [];
    public List<LookUpItemDTO> RoiRanges { get; set; } = [];
    public List<LookUpItemDTO> RoiTypes { get; set; } = [];
    //public List<LookUpItemDTO> Banks { get; internal set; }
}
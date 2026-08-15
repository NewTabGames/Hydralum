using System;

namespace MalumMenu;

[Serializable]
public class MalumOutfit
{
    public string Name { get; set; } = "New Outfit";
    public int ColorId { get; set; } = 0;
    public string HatId { get; set; } = "";
    public string VisorId { get; set; } = "";
    public string SkinId { get; set; } = "";
    public string PetId { get; set; } = "";
    public string NamePlateId { get; set; } = "";
    public string CreatedDate { get; set; } = "";
}

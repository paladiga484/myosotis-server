namespace Database.Entities;

public sealed class Formation
{
    public long FormationId { get; set; }
    public long Uid { get; set; }
    public int Id { get; set; }
}

public sealed class FormationName
{
    public long FormationId { get; set; }
    public int K { get; set; }
    public int V { get; set; }
}

public sealed class FormationDetail
{
    public long FormationId { get; set; }
    public int PersonalityId { get; set; }
    public bool IsParticipated { get; set; }
    public int ParticipationOrder { get; set; }
    public int SkinId { get; set; }
}

public sealed class FormationEgo
{
    public long FormationId { get; set; }
    public int PersonalityId { get; set; }
    public int Idx { get; set; }
    public int EgoId { get; set; }
}

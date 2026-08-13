using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Types.Server;

namespace Database.Repositories;

public sealed class FormationRepository(
    MyosotisDbContext db,
    DatabaseService databaseService,
    ILogger<FormationRepository> logger) : RepositoryBase(db, databaseService, logger)
{
    public async Task<List<FormationFormat>> ListAsync(long uid)
    {
        var formations = await Db.Formations.AsNoTracking()
            .Where(f => f.Uid == uid)
            .OrderBy(f => f.Id)
            .ToListAsync();

        var result = new List<FormationFormat>(formations.Count);
        foreach (var formation in formations)
            result.Add(await AssembleAsync(formation));

        return result;
    }

    public async Task<FormationFormat?> GetAsync(long uid, int formationId)
    {
        var formation = await Db.Formations.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Uid == uid && f.Id == formationId);
        return formation is null ? null : await AssembleAsync(formation);
    }

    /// <summary>Replaces a formation's names/details/egos from the client payload (one SaveChanges).</summary>
    public async Task<bool> UpdateAsync(long uid, int formationId, FormationFormat updated)
    {
        var formation = await Db.Formations.FirstOrDefaultAsync(f => f.Uid == uid && f.Id == formationId);
        if (formation is null)
            return true;

        var fid = formation.FormationId;
        Db.FormationNames.RemoveRange(Db.FormationNames.Where(x => x.FormationId == fid));
        Db.FormationDetails.RemoveRange(Db.FormationDetails.Where(x => x.FormationId == fid));
        Db.FormationEgos.RemoveRange(Db.FormationEgos.Where(x => x.FormationId == fid));

        Db.FormationNames.AddRange(updated.formationNameFormat.Select(n =>
            new FormationName { FormationId = fid, K = n.k, V = n.v }));

        foreach (var detail in updated.formationDetails)
        {
            Db.FormationDetails.Add(new FormationDetail
            {
                FormationId = fid,
                PersonalityId = detail.personalityId,
                IsParticipated = detail.isParticipated,
                ParticipationOrder = detail.participationOrder,
                SkinId = detail.skinId,
            });
            for (int i = 0; i < detail.egos.Count; i++)
                Db.FormationEgos.Add(new FormationEgo
                {
                    FormationId = fid,
                    PersonalityId = detail.personalityId,
                    Idx = i,
                    EgoId = detail.egos[i],
                });
        }

        return await SaveAsync(uid);
    }

    private async Task<FormationFormat> AssembleAsync(Formation formation)
    {
        var fid = formation.FormationId;
        var names = await Db.FormationNames.AsNoTracking()
            .Where(x => x.FormationId == fid)
            .OrderBy(x => x.K)
            .ToListAsync();
        var details = await Db.FormationDetails.AsNoTracking()
            .Where(x => x.FormationId == fid)
            .OrderBy(x => x.ParticipationOrder)
            .ToListAsync();
        var egos = await Db.FormationEgos.AsNoTracking()
            .Where(x => x.FormationId == fid)
            .OrderBy(x => x.PersonalityId)
            .ThenBy(x => x.Idx)
            .ToListAsync();

        return new FormationFormat
        {
            id = formation.Id,
            formationDetails = details.Select(d => new FormationDetailFormat
            {
                personalityId = d.PersonalityId,
                egos = egos.Where(e => e.PersonalityId == d.PersonalityId).Select(e => e.EgoId).ToList(),
                isParticipated = d.IsParticipated,
                participationOrder = d.ParticipationOrder,
                skinId = d.SkinId,
            }).ToList(),
            formationNameFormat = names.Select(n => new FormationNameElement { k = n.K, v = n.V }).ToList(),
        };
    }
}

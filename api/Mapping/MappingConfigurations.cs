using Mapster;
using api.Entities;
using api.Contracts.Users;
using api.Contracts.Journals;
using api.Contracts.AI;
using api.Contracts.Exercises;

namespace api.Mapping;

public class MappingConfigurations : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // User → UserResponse
        config.NewConfig<User, UserResponse>()
            .Map(dest => dest.Parameters,
                 src  => src.ParameterSnapshot != null ? src.ParameterSnapshot.ToParametersDictionary() : null)
            .Map(dest => dest.ParametersUpdatedAt,
                 src  => src.ParameterSnapshot != null ? (DateTime?)src.ParameterSnapshot.UpdatedAt : null)
            .Map(dest => dest.TotalJournalEntries, src => 0); // populated by service

        // JournalEntry → JournalSummaryResponse
        config.NewConfig<JournalEntry, JournalSummaryResponse>()
            .Map(dest => dest.Tags, src => src.JournalTags.Select(t => t.Tag).ToArray())
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

        // SuggestedExercise → SuggestedExerciseResponse
        config.NewConfig<SuggestedExercise, SuggestedExerciseResponse>()
            .Map(dest => dest.Id,           src => src.ExerciseCode)
            .Map(dest => dest.Parameter,    src => src.Parameter)
            .Map(dest => dest.Score,        src => src.Score)
            .Map(dest => dest.ScoreRange,   src => src.ScoreRange);

        // SuggestedExercise → ExerciseResponse
        config.NewConfig<SuggestedExercise, ExerciseResponse>()
            .Map(dest => dest.Id,           src => src.Id)
            .Map(dest => dest.ExerciseCode, src => src.ExerciseCode)
            .Map(dest => dest.Parameter,    src => src.Parameter)
            .Map(dest => dest.Score,        src => src.Score)
            .Map(dest => dest.ScoreRange,   src => src.ScoreRange)
            .Map(dest => dest.JournalEntryId, src => src.JournalEntryId);
    }
}

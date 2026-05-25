using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using api.Entities;

namespace api.Persistence.EntitiesConfigurations;

public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.ExerciseType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Difficulty).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Instructions).IsRequired();
        builder.Property(x => x.ApplicableParameters).HasConversion(
            v => string.Join(",", v),
            v => v.Length == 0
                 ? new List<string>()
                 : v.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList(),
            new ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        var d = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new Exercise { Id = 1, Name = "CBT Thought Record",            ExerciseType = "CBT",        DurationMinutes = 15, Difficulty = "intermediate", ApplicableParameters = ["ANX", "DEP", "CDT"],  IsActive = true, CreatedAt = d, Description = "Record and challenge negative thoughts using cognitive behavioural therapy.",    Instructions = "Write the automatic thought. Rate belief (0–100%). List evidence for/against. Write a balanced thought. Re-rate belief." },
            new Exercise { Id = 2, Name = "Box Breathing",                 ExerciseType = "Breathing",  DurationMinutes = 5,  Difficulty = "beginner",     ApplicableParameters = ["ANX", "STR"],         IsActive = true, CreatedAt = d, Description = "Calm the nervous system with a 4-4-4-4 breathing pattern.",                       Instructions = "Inhale 4 counts → hold 4 → exhale 4 → hold 4. Repeat 4 cycles." },
            new Exercise { Id = 3, Name = "Sleep Hygiene Checklist",       ExerciseType = "Sleep",      DurationMinutes = 10, Difficulty = "beginner",     ApplicableParameters = ["SLP", "ANX"],         IsActive = true, CreatedAt = d, Description = "Improve sleep quality with evidence-based pre-sleep practices.",                   Instructions = "No screens 1 hr before bed. Cool dark room. Fixed sleep/wake time. No caffeine after noon." },
            new Exercise { Id = 4, Name = "Behavioural Activation",        ExerciseType = "Behavioral", DurationMinutes = 20, Difficulty = "beginner",     ApplicableParameters = ["DEP", "ENG"],         IsActive = true, CreatedAt = d, Description = "Overcome low mood through structured enjoyable activity scheduling.",              Instructions = "List 5 activities you used to enjoy. Schedule one for today. Note mood before and after." },
            new Exercise { Id = 5, Name = "Progressive Muscle Relaxation", ExerciseType = "Relaxation", DurationMinutes = 15, Difficulty = "beginner",     ApplicableParameters = ["STR", "ANX", "SLP"],  IsActive = true, CreatedAt = d, Description = "Reduce tension by systematically tensing and releasing muscle groups.",            Instructions = "Start from feet — tense each group for 5 s then release. Work up to the face." },
            new Exercise { Id = 6, Name = "Social Connection Task",        ExerciseType = "Social",     DurationMinutes = 10, Difficulty = "beginner",     ApplicableParameters = ["SOC"],                IsActive = true, CreatedAt = d, Description = "Combat isolation with a small, low-pressure social connection.",                   Instructions = "Send one message to someone you trust. No expectation of a reply needed." },
            new Exercise { Id = 7, Name = "Safety Planning",               ExerciseType = "Safety",     DurationMinutes = 30, Difficulty = "intermediate", ApplicableParameters = ["SAFE"],               IsActive = true, CreatedAt = d, Description = "Create a personal crisis plan with coping strategies and support contacts.",      Instructions = "List warning signs → coping strategies → trusted contacts → professional services. Review with a clinician." },
            new Exercise { Id = 8, Name = "Gratitude Journaling",          ExerciseType = "Mindfulness",DurationMinutes = 5,  Difficulty = "beginner",     ApplicableParameters = ["ENG", "DEP"],         IsActive = true, CreatedAt = d, Description = "Boost engagement by recording three good things each day.",                       Instructions = "Each evening write three things that went well today and why they happened." }
        );
    }
}

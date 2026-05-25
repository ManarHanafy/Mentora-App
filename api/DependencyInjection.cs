using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentValidation;
using FluentValidation.AspNetCore;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using api.Authentication;
using api.Authorization;
using api.Contracts.Crisis;
using api.HealthChecks;
using api.Infrastructure.Audit;
using api.Infrastructure.BackgroundJobs;
using api.Infrastructure.Caching;
using api.Infrastructure.Email;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;
using api.Infrastructure.Serialization;

namespace api;

public static class DependencyInjection
{
    public static IServiceCollection AddDependencies(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeJsonConverter());
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var validationErrors = context.ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(e => (object)new
                        {
                            Code = "Validation.Invalid",
                            Field = x.Key,
                            Description = string.IsNullOrWhiteSpace(e.ErrorMessage)
                                ? "The input was not valid."
                                : e.ErrorMessage
                        }))
                        .ToList();

                    var problem = api.Errors.ApiProblemDetailsFactory.Create(
                        context.HttpContext,
                        StatusCodes.Status400BadRequest,
                        "Validation Failed",
                        "One or more validation errors occurred.",
                        validationErrors: validationErrors);

                    return new BadRequestObjectResult(problem);
                };
            });

        services.AddOptions<EmailOptions>()
            .BindConfiguration(EmailOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddCorsConfig(configuration, environment);
        services.AddAuthConfig(configuration);
        services.AddAuthorizationConfig();
        services.AddRateLimiterConfig(configuration);
        services.AddHealthChecksConfig();
        services.AddDistributedMemoryCache();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services
            .AddSwaggerServices()
            .AddMapsterConfig()
            .AddFluentValidationConfig()
            .AddApplicationServices()
            .AddAIHttpClient(configuration);

        services.AddHttpContextAccessor();
        services.AddExceptionHandler<api.Errors.GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService,       UserService>();
        services.AddScoped<IJournalService,    JournalService>();
        services.AddScoped<IAuthService,       AuthService>();
        services.AddScoped<IExerciseService,   ExerciseService>();
        services.AddScoped<IChatService,       ChatService>();
        services.AddScoped<IAccountService,    AccountService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<ICrisisResourceService, CrisisResourceService>();
        services.AddScoped<IMoodService,       MoodService>();
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddSingleton<api.Authentication.IJwtProvider, api.Authentication.JwtProvider>();
        services.AddSingleton<IOnboardingScoringEngine, OnboardingScoringEngine>();
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        services.AddHostedService<QueuedHostedService>();
        services.AddHostedService<InactiveChatCleanupJob>();
        services.AddSingleton<IAppCacheService, AppCacheService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        return services;
    }

    private static IServiceCollection AddAuthConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Key)
                    && options.Key.Length >= 32
                    && !string.Equals(
                        options.Key,
                        "your-256-bit-secret-key-at-least-32-characters-long-replace-this-in-production!!!",
                        StringComparison.Ordinal),
                "JWT signing key must be configured with a non-default value that is at least 32 characters long.")
            .ValidateOnStart();

        services.AddOptions<CrisisResourcesOptions>()
            .BindConfiguration(CrisisResourcesOptions.SectionName)
            .ValidateOnStart();

        var jwtSettings = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT settings not found in configuration.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(o =>
        {
            o.SaveToken = true;
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                ValidIssuer              = jwtSettings.Issuer,
                ValidAudience            = jwtSettings.Audience,
                ClockSkew                = TimeSpan.Zero
            };
        });

        return services;
    }

    private static IServiceCollection AddAuthorizationConfig(this IServiceCollection services)
    {
        services.AddAuthorizationPolicies();
        services.AddScoped<IAuditLogger, AuditLogger>();
        return services;
    }

    private static IServiceCollection AddRateLimiterConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue<int>("RateLimiting:PermitLimit", 120);
        var windowSeconds = configuration.GetValue<int>("RateLimiting:WindowSeconds", 60);
        var queueLimit = configuration.GetValue<int>("RateLimiting:QueueLimit", 20);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = windowSeconds.ToString();
                return ValueTask.CompletedTask;
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.IsAuthenticated == true
                        ? context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous"
                        : context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        QueueLimit = queueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));
        });

        return services;
    }

    private static IServiceCollection AddHealthChecksConfig(this IServiceCollection services)
    {
        services.AddHttpClient(nameof(AIServiceHealthCheck));

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
            .AddCheck<AIServiceHealthCheck>("ai-provider", tags: ["ready"]);

        return services;
    }

    private static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title   = "Mental Health API",
                Version = "v1",
                Description = "API for the Mental Health application."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type        = SecuritySchemeType.Http,
                Scheme      = "bearer",
                BearerFormat = "JWT",
                Description = "Enter your JWT token. Example: eyJhbGci..."
            });

            options.AddSecurityRequirement(doc =>
            {
                doc.RegisterComponents();
                var requirement = new OpenApiSecurityRequirement();
                requirement.Add(new OpenApiSecuritySchemeReference("Bearer", doc, null), new List<string>());
                return requirement;
            });
        });
        return services;
    }

    private static IServiceCollection AddMapsterConfig(this IServiceCollection services)
    {
        var mappingConfig = TypeAdapterConfig.GlobalSettings;
        mappingConfig.Scan(Assembly.GetExecutingAssembly());

        services.AddSingleton(mappingConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }

    private static IServiceCollection AddFluentValidationConfig(this IServiceCollection services)
    {
        services
            .AddFluentValidationAutoValidation()
            .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }

    private static IServiceCollection AddCorsConfig(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>();

        services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
            {
                var builder = policy.AllowAnyMethod().AllowAnyHeader();

                if (allowedOrigins is { Length: > 0 })
                    builder.WithOrigins(allowedOrigins);
                else if (environment.IsDevelopment())
                    builder.AllowAnyOrigin();
                else
                    throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one allowed origin outside Development.");
            }));

        return services;
    }

    private static IServiceCollection AddAIHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["MentoraAI:BaseUrl"] ?? "https://mentorra.pythonanywhere.com";
        var timeoutSeconds = configuration.GetValue<int>("MentoraAI:TimeoutSeconds", 30);

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException("MentoraAI:BaseUrl must be a valid absolute URI.");

        if (timeoutSeconds <= 0)
            throw new InvalidOperationException("MentoraAI:TimeoutSeconds must be greater than 0.");

        services.AddHttpClient<IAIService, RealAIService>(client =>
        {
            client.BaseAddress = baseUri;
            // Keep HttpClient timeout above resilience policy timeouts
            // to allow the resilience pipeline (attempt + total timeout) to control failures consistently.
            client.Timeout     = TimeSpan.FromSeconds(timeoutSeconds + 10);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(timeoutSeconds + 5);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            // Ensure sampling duration is at least double the attempt timeout to satisfy validation
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(timeoutSeconds * 2);
        });

        return services;
    }
}

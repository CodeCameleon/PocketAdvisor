using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PocketAdvisor.DbContexts;
using PocketAdvisor.DbContexts.Extensions;
using PocketAdvisor.Entities;
using PocketAdvisor.Repositories.Extensions;
using PocketAdvisor.Requests.Users;
using PocketAdvisor.Services.Extensions;
using PocketAdvisor.WebApplication.Extensions;
using PocketAdvisor.WebApplication.Middlewares;

// Creates a new web application builder instance.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Loads the application secrets from the secure store.
builder.AddPocketAdvisorSecrets();

// Adds the database context to the container.
builder.Services.AddPocketAdvisorDbContext(
    builder.Configuration.GetDefaultConnectionString()
);

// Adds the transaction manager to the container.
builder.Services.AddTransactionManager();

// Adds the repositories to the container.
builder.Services.AddRepositories();

// Adds the password hasher to the container.
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Adds the validators to the container.
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

// Adds the Resend client to the container.
builder.Services.AddResendClient(
    builder.Configuration.GetResendApiKey()
);

// Adds the services to the container.
builder.Services.AddServices(builder.Configuration);

// Adds the API controllers to the container.
builder.Services.AddControllers();

// Adds the Swagger services to the container.
builder.Services.AddPocketAdvisorSwagger();

// Builds the web application.
WebApplication app = builder.Build();

// Applies any pending migrations to the database.
using (IServiceScope scope = app.Services.CreateScope())
{
    IServiceProvider serviceProvider = scope.ServiceProvider;
    PocketAdvisorDbContext context = serviceProvider.GetRequiredService<PocketAdvisorDbContext>();
    await context.Database.MigrateAsync();
}

// Adds the middleware for handling exceptions.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Adds the middleware for Swagger generation.
app.UsePocketAdvisorSwagger();

// Adds the middleware for redirecting HTTP requests.
app.UseHttpsRedirection();

// Adds the middleware for authorization.
app.UseAuthorization();

// Adds the endpoints for controller actions.
app.MapControllers();

// Starts the application.
app.Run();

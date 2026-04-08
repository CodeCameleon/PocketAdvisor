using FluentValidation;
using PocketAdvisor.DbContexts;
using PocketAdvisor.DbContexts.Extensions;
using PocketAdvisor.Repositories.Extensions;
using PocketAdvisor.Requests.Users;
using PocketAdvisor.WebApplication.Extensions;

// Creates a new web application builder instance.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Adds the database context to the container.
builder.Services.AddPocketAdvisorDbContext(
    builder.Configuration.GetDefaultConnectionString()
);

// Adds the transaction manager to the container.
builder.Services.AddTransactionManager();

// Adds the repositories to the container.
builder.Services.AddRepositories();

// Adds the validators to the container.
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

// Adds the API controllers to the container.
builder.Services.AddControllers();

// Adds the Swagger services to the container.
builder.Services.AddPocketAdvisorSwagger();

// Builds the web application.
WebApplication app = builder.Build();

// Ensures the database exists.
using (IServiceScope scope = app.Services.CreateScope())
{
    IServiceProvider serviceProvider = scope.ServiceProvider;
    PocketAdvisorDbContext context = serviceProvider.GetRequiredService<PocketAdvisorDbContext>();
    context.Database.EnsureCreated();
}

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

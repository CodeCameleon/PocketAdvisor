// Creates a new web application builder instance.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Adds the API controllers to the container.
builder.Services.AddControllers();

// Adds the OpenAPI services to the container.
builder.Services.AddOpenApi();

// Builds the web application.
WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Exposes the OpenAPI documentation endpoint.
    app.MapOpenApi();
}

// Adds the middleware for redirecting HTTP requests.
app.UseHttpsRedirection();

// Adds the middleware for authorization.
app.UseAuthorization();

// Adds the endpoints for controller actions.
app.MapControllers();

// Starts the application.
app.Run();

using DispatchEndpoints;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services
    .AddControllers()
    .AddDispatchEndpoints()
    .AddEndpointsApiExplorer()
    .AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
